using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Dragon.Resource;
using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// 从对象池取出 / 归还时由实现者做重置逻辑（清空引用、停粒子等）。
    /// </summary>
    public interface IPoolReset
    {
        void OnSpawnFromPool();
        void OnReturnToPool();
    }

    /// <summary>
    /// 按池标识管理 GameObject 预制体实例的通用对象池。
    /// </summary>
    public sealed class PoolManager
    {
        #region //静态变量

        private static PoolManager _instance;

        public static PoolManager Instance => _instance ??= new PoolManager();

        #endregion

        private readonly Dictionary<string, PrefabPool> _pools = new Dictionary<string, PrefabPool>();
        private Transform _poolRoot;

        private PoolManager()
        {
        }

        private Transform PoolRootTransform
        {
            get
            {
                if (_poolRoot == null)
                {
                    var go = new GameObject("[PoolManager]");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    _poolRoot = go.transform;
                }

                return _poolRoot;
            }
        }

        /// <summary>
        /// 注册池并可选预热。同一 poolId 重复注册会忽略并打日志。
        /// </summary>
        /// <param name="poolId">唯一池标识</param>
        /// <param name="prefab">预制体（需为场景外模板，勿用已挂场景实例作模板）</param>
        /// <param name="prewarm">预先创建的闲置数量</param>
        /// <param name="maxInactive">闲置栈上限，超过则在归还时销毁多余实例（≤0 表示不限制）</param>
        public void Register(string poolId, GameObject prefab, int prewarm = 0, int maxInactive = 0)
        {
            if (string.IsNullOrEmpty(poolId))
            {
                Debug.LogError("[PoolManager] poolId 为空");
                return;
            }

            if (prefab == null)
            {
                Debug.LogError($"[PoolManager] prefab 为空: {poolId}");
                return;
            }

            if (_pools.ContainsKey(poolId))
            {
                Debug.LogWarning($"[PoolManager] 池已存在，跳过重复注册: {poolId}");
                return;
            }

            var pool = new PrefabPool(poolId, prefab, PoolRootTransform, prewarm, maxInactive);
            _pools.Add(poolId, pool);
        }

        /// <summary>
        /// 通过 <see cref="ResourceManager"/>（Addressables）异步加载预制体并注册池。
        /// 池 ID 与传入的 <paramref name="addressablePath"/> 一致，后续 <see cref="Get"/> / <see cref="Release"/> 均使用该路径字符串（与配置表、Addressables 寻址一致）。
        /// </summary>
        /// <returns>已存在或注册成功为 true；路径无效、加载失败或结果为空为 false</returns>
        public async UniTask<bool> RegisterAsync(string addressablePath, int prewarm = 0, int maxInactive = 0)
        {
            if (string.IsNullOrEmpty(addressablePath))
            {
                Debug.LogError("[PoolManager] RegisterAsync: addressablePath 为空");
                return false;
            }

            if (_pools.ContainsKey(addressablePath))
            {
                Debug.LogWarning($"[PoolManager] 池已存在，跳过异步注册: {addressablePath}");
                return true;
            }

            try
            {
                GameObject prefab = await ResourceManager.Instance.LoadAssetAsync<GameObject>(addressablePath);
                if (prefab == null)
                {
                    Debug.LogError($"[PoolManager] RegisterAsync 加载结果为空: {addressablePath}");
                    return false;
                }

                Register(addressablePath, prefab, prewarm, maxInactive);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PoolManager] RegisterAsync 加载失败: {addressablePath}, {ex}");
                return false;
            }
        }

        public bool IsRegistered(string poolId) =>
            !string.IsNullOrEmpty(poolId) && _pools.ContainsKey(poolId);

        public GameObject Get(string poolId, Transform parent = null, bool worldPositionStays = false)
        {
            if (!_pools.TryGetValue(poolId, out var pool))
            {
                Debug.LogError($"[PoolManager] 未注册的池: {poolId}");
                return null;
            }

            return pool.Get(parent, worldPositionStays);
        }

        public T Get<T>(string poolId, Transform parent = null, bool worldPositionStays = false)
            where T : Component
        {
            var go = Get(poolId, parent, worldPositionStays);
            return go != null ? go.GetComponent<T>() : null;
        }

        /// <summary>
        /// 归还实例；若无 <see cref="PoolMember"/> 或池已注销则销毁实例。
        /// </summary>
        public void Release(GameObject instance)
        {
            if (instance == null) return;

            var member = instance.GetComponent<PoolMember>();
            if (member == null || string.IsNullOrEmpty(member.PoolId))
            {
                Debug.LogWarning("[PoolManager] Release 的对象缺少 PoolMember，将直接销毁");
                UnityEngine.Object.Destroy(instance);
                return;
            }

            if (!_pools.TryGetValue(member.PoolId, out var pool))
            {
                UnityEngine.Object.Destroy(instance);
                return;
            }

            pool.Release(instance);
        }

        public void Unregister(string poolId, bool destroyInstances = true)
        {
            if (!_pools.TryGetValue(poolId, out var pool)) return;

            if (destroyInstances)
                pool.Dispose();
            _pools.Remove(poolId);
        }

        /// <summary>
        /// 注销并销毁所有池内实例（例如退出游戏时）。
        /// </summary>
        public void ClearAll()
        {
            foreach (var kv in _pools)
                kv.Value.Dispose();
            _pools.Clear();
        }

        private sealed class PrefabPool
        {
            private readonly string _poolId;
            private readonly GameObject _prefab;
            private readonly Transform _hideRoot;
            private readonly int _maxInactive;
            private readonly Stack<GameObject> _inactive = new Stack<GameObject>();
            private readonly HashSet<GameObject> _instances = new HashSet<GameObject>();

            public PrefabPool(string poolId, GameObject prefab, Transform hideRoot, int prewarm, int maxInactive)
            {
                _poolId = poolId;
                _prefab = prefab;
                _hideRoot = hideRoot;
                _maxInactive = maxInactive;

                for (var i = 0; i < prewarm; i++)
                {
                    var go = CreateInstance();
                    go.SetActive(false);
                    _inactive.Push(go);
                }
            }

            private GameObject CreateInstance()
            {
                var go = UnityEngine.Object.Instantiate(_prefab, _hideRoot);
                go.name = _prefab.name;
                var member = go.GetComponent<PoolMember>();
                if (member == null)
                    member = go.AddComponent<PoolMember>();
                member.PoolId = _poolId;
                _instances.Add(go);
                return go;
            }

            public GameObject Get(Transform parent, bool worldPositionStays)
            {
                GameObject go;
                if (_inactive.Count > 0)
                    go = _inactive.Pop();
                else
                    go = CreateInstance();

                go.transform.SetParent(parent, worldPositionStays);
                go.SetActive(true);
                NotifySpawn(go);
                return go;
            }

            public void Release(GameObject go)
            {
                if (go == null || !_instances.Contains(go)) return;

                NotifyReturn(go);
                go.SetActive(false);
                go.transform.SetParent(_hideRoot, false);

                if (_maxInactive > 0 && _inactive.Count >= _maxInactive)
                {
                    _instances.Remove(go);
                    UnityEngine.Object.Destroy(go);
                    return;
                }

                _inactive.Push(go);
            }

            public void Dispose()
            {
                foreach (var go in _instances)
                {
                    if (go != null)
                        UnityEngine.Object.Destroy(go);
                }

                _instances.Clear();
                _inactive.Clear();
            }

            private static void NotifySpawn(GameObject go)
            {
                var resets = go.GetComponentsInChildren<IPoolReset>(true);
                for (var i = 0; i < resets.Length; i++)
                    resets[i].OnSpawnFromPool();
            }

            private static void NotifyReturn(GameObject go)
            {
                var resets = go.GetComponentsInChildren<IPoolReset>(true);
                for (var i = resets.Length - 1; i >= 0; i--)
                    resets[i].OnReturnToPool();
            }
        }
    }
}
