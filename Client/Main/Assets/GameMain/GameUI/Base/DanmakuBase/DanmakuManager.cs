using System;
using System.Collections.Generic;
using Dragon.Resource;
using UnityEngine;

namespace Dragon.GameUI
{
    /// <summary>
    /// 弹幕管理器：单轨道右→左滚动，队列错开生成避免重叠。
    /// 内容物由调用方通过回调或已有 GameObject 自定义，无快捷图文 API。
    /// </summary>
    public class DanmakuManager
    {
        public const string DefaultDanmakuAddress = "ui_danmaku";

        public float DefaultTopOffset = 100f;
        public float DefaultGap = 40f;
        public float DefaultSpeed = 280f;

        #region Singleton

        private static DanmakuManager _instance;
        private static DanmakuRoot _danmakuRoot;

        public static DanmakuManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DanmakuManager();
                    _danmakuRoot = DanmakuRoot.Create();
                    _danmakuRoot.Init();
                }

                return _instance;
            }
        }

        #endregion

        private readonly Queue<PendingDanmaku> _pendingQueue = new Queue<PendingDanmaku>();
        private UIDanmakuItem _latestItem;
        private bool _canSpawnNext = true;
        private bool _spawnInFlight;

        /// <summary>
        /// 从 Addressables 加载预制体并入队显示；<paramref name="bind"/> 在实例化后、开始滚动前调用。
        /// 滚出后默认销毁；<paramref name="onComplete"/> 仅作完成通知。
        /// </summary>
        public void Show(
            string danmakuAddress,
            Action<GameObject> bind = null,
            float topOffset = -1f,
            float gap = -1f,
            float speed = -1f,
            Action onComplete = null)
        {
            if (string.IsNullOrEmpty(danmakuAddress))
            {
                Debug.LogError("[DanmakuManager] Show failed, danmakuAddress is empty.");
                return;
            }

            Enqueue(new PendingDanmaku
            {
                Address = danmakuAddress,
                Bind = bind,
                TopOffset = ResolveTopOffset(topOffset),
                Gap = ResolveGap(gap),
                Speed = ResolveSpeed(speed),
                OnComplete = onComplete,
            });
        }

        /// <summary>
        /// 直接使用已有 GameObject 入队显示（无 UIDanmakuItem 时自动添加）。
        /// 滚出后默认销毁；<paramref name="onComplete"/> 仅作完成通知。
        /// </summary>
        public void Show(
            GameObject content,
            float topOffset = -1f,
            float gap = -1f,
            float speed = -1f,
            Action onComplete = null)
        {
            if (content == null)
            {
                Debug.LogError("[DanmakuManager] Show failed, content is null.");
                return;
            }

            Enqueue(new PendingDanmaku
            {
                ExistingObject = content,
                TopOffset = ResolveTopOffset(topOffset),
                Gap = ResolveGap(gap),
                Speed = ResolveSpeed(speed),
                OnComplete = onComplete,
            });
        }

        private void Enqueue(PendingDanmaku pending)
        {
            _pendingQueue.Enqueue(pending);
            TryDequeueAndSpawn();
        }

        private void TryDequeueAndSpawn()
        {
            if (!_canSpawnNext || _spawnInFlight || _pendingQueue.Count == 0)
            {
                return;
            }

            if (_danmakuRoot == null)
            {
                Debug.LogError("[DanmakuManager] Spawn failed, DanmakuRoot is not ready.");
                return;
            }

            PendingDanmaku pending = _pendingQueue.Dequeue();
            _canSpawnNext = false;
            _spawnInFlight = true;

            if (pending.ExistingObject != null)
            {
                SpawnReadyObject(pending.ExistingObject, pending);
                return;
            }

            string address = pending.Address;
            ResourceManager.Instance.LoadAsync<GameObject>(address, prefab =>
            {
                if (prefab == null)
                {
                    Debug.LogError($"[DanmakuManager] Show failed, prefab not found: {address}");
                    OnSpawnFailed();
                    return;
                }

                if (_danmakuRoot == null)
                {
                    Debug.LogError("[DanmakuManager] Show failed, DanmakuRoot is not ready.");
                    OnSpawnFailed();
                    return;
                }

                GameObject instance = UnityEngine.Object.Instantiate(prefab, _danmakuRoot.GetDanmakuRootTransform());
                instance.name = prefab.name;
                pending.Bind?.Invoke(instance);
                SpawnReadyObject(instance, pending);
            }, ex =>
            {
                Debug.LogError($"[DanmakuManager] Show failed to load prefab: {address}, {ex.Message}");
                OnSpawnFailed();
            });
        }

        private void SpawnReadyObject(GameObject content, PendingDanmaku pending)
        {
            if (content == null)
            {
                OnSpawnFailed();
                return;
            }

            Transform rootTransform = _danmakuRoot.GetDanmakuRootTransform();
            if (content.transform.parent != rootTransform)
            {
                content.transform.SetParent(rootTransform, false);
            }

            UIDanmakuItem item = content.GetComponent<UIDanmakuItem>();
            if (item == null)
            {
                item = content.AddComponent<UIDanmakuItem>();
            }

            RectTransform rootRect = _danmakuRoot.GetDanmakuRootRect();
            _latestItem = item;
            _spawnInFlight = false;

            Action onComplete = pending.OnComplete;
            item.Play(
                rootRect,
                pending.TopOffset,
                pending.Gap,
                pending.Speed,
                OnItemFullyEntered,
                completedItem =>
                {
                    onComplete?.Invoke();
                    OnItemCompleted(completedItem);
                });
        }

        private void OnItemFullyEntered(UIDanmakuItem item)
        {
            if (item != _latestItem)
            {
                return;
            }

            _canSpawnNext = true;
            TryDequeueAndSpawn();
        }

        private void OnItemCompleted(UIDanmakuItem item)
        {
            if (item == _latestItem)
            {
                _latestItem = null;
                // 未完整入屏就结束，且当前没有在飞的生成，则放行队列
                if (!_canSpawnNext && !_spawnInFlight)
                {
                    _canSpawnNext = true;
                    TryDequeueAndSpawn();
                }
            }
        }

        private void OnSpawnFailed()
        {
            _spawnInFlight = false;
            _canSpawnNext = true;
            TryDequeueAndSpawn();
        }

        private float ResolveTopOffset(float topOffset)
        {
            return topOffset >= 0f ? topOffset : DefaultTopOffset;
        }

        private float ResolveGap(float gap)
        {
            return gap >= 0f ? gap : DefaultGap;
        }

        private float ResolveSpeed(float speed)
        {
            return speed > 0f ? speed : DefaultSpeed;
        }

        private sealed class PendingDanmaku
        {
            public string Address;
            public GameObject ExistingObject;
            public Action<GameObject> Bind;
            public float TopOffset;
            public float Gap;
            public float Speed;
            public Action OnComplete;
        }
    }
}
