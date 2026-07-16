using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Dragon.Resource
{
    /// <summary>
    /// 资源管理器统计信息
    /// </summary>
    public class ResourceManagerStats
    {
        public int TotalCachedResources { get; set; }
        public int TrackedReferenceCounts { get; set; }

        public override string ToString()
        {
            return $"总缓存资源: {TotalCachedResources}, 引用计数: {TrackedReferenceCounts}";
        }
    }

    /// <summary>
    /// 预加载任务状态
    /// </summary>
    public enum PreloadTaskStatus
    {
        Pending,
        Loading,
        Completed,
        Failed
    }

    /// <summary>
    /// 预加载任务信息
    /// </summary>
    public class PreloadTaskInfo
    {
        public string ResourcePath { get; set; }
        public PreloadTaskStatus Status { get; set; }
        public Exception Error { get; set; }
    }

    /// <summary>
    /// 异步加载等待回调（同一地址+类型并发请求合并时使用）
    /// </summary>
    internal class PendingLoadCallback
    {
        public Action<UnityEngine.Object> OnComplete;
        public Action<Exception> OnFail;
    }

    /// <summary>
    /// 资源管理器，基于 Addressables 统一管理游戏中的资源加载、卸载和缓存
    /// </summary>
    public class ResourceManager
    {
        private const char CacheKeySeparator = '\0';

        #region //静态变量

        //静态变量
        protected static ResourceManager _instance = null;

        //静态属性
        public static ResourceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ResourceManager();
                }
                return _instance;
            }
        }
        #endregion

        private static SpriteLoader SpriteLoader;

        // 引用计数，键为 path + 类型
        private readonly Dictionary<string, int> _referenceCounts = new Dictionary<string, int>();

        // 资源缓存实例
        private IResourceCache _resourceCache;

        // 存储正在进行中的异步加载请求，防止重复加载同一资源（键为 path + 类型）
        private readonly Dictionary<string, List<PendingLoadCallback>> _pendingLoadRequests = new Dictionary<string, List<PendingLoadCallback>>();

        // 加载中的 Addressables 句柄（尚未入库），供 ClearAll 释放
        private readonly Dictionary<string, AsyncOperationHandle> _loadingHandles = new Dictionary<string, AsyncOperationHandle>();

        // Addressables 资源句柄字典，用于正确释放资源
        private readonly Dictionary<string, IReleasableResource> _addressableHandles = new Dictionary<string, IReleasableResource>();

        public void Initialize()
        {
            SpriteLoader = SpriteLoader.Instance;
            SpriteLoader.Init();
            _resourceCache = new ResourceCache();
        }

        /// <summary>
        /// 缓存键：地址 + 资源类型，避免同地址不同 T 互相覆盖
        /// </summary>
        private static string MakeCacheKey<T>(string path) where T : UnityEngine.Object
        {
            return MakeCacheKey(path, typeof(T));
        }

        private static string MakeCacheKey(string path, Type type)
        {
            return string.Concat(path, CacheKeySeparator, type.FullName);
        }

        private static bool TryGetPathFromCacheKey(string cacheKey, out string path)
        {
            int index = cacheKey.IndexOf(CacheKeySeparator);
            if (index < 0)
            {
                path = cacheKey;
                return false;
            }

            path = cacheKey.Substring(0, index);
            return true;
        }

        private List<string> FindCacheKeysForPath(string path)
        {
            var keys = new List<string>();

            foreach (var key in _referenceCounts.Keys)
            {
                if (TryGetPathFromCacheKey(key, out var keyPath) && keyPath == path)
                {
                    keys.Add(key);
                }
            }

            foreach (var key in _addressableHandles.Keys)
            {
                if (TryGetPathFromCacheKey(key, out var keyPath) && keyPath == path && !keys.Contains(key))
                {
                    keys.Add(key);
                }
            }

            return keys;
        }

        /// <summary>
        /// 同步加载资源（内部使用 WaitForCompletion，可能造成卡顿，优先用 LoadAsync）
        /// </summary>
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            string cacheKey = MakeCacheKey<T>(path);

            if (_resourceCache.TryGetResource<T>(cacheKey, out T cachedResource))
            {
                AddReference(cacheKey);
                return cachedResource;
            }

            // 缓存未命中但残留句柄（缓存层曾清掉失效对象），先释放再加载
            ReleaseStaleHandleIfNeeded(cacheKey);

            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(path);
            T resource = handle.WaitForCompletion();

            if (handle.Status == AsyncOperationStatus.Succeeded && resource != null)
            {
                _addressableHandles[cacheKey] = new AddressableResourceHandle<T>(handle);
                _resourceCache.AddResource(cacheKey, resource);
                _referenceCounts[cacheKey] = 1;
                return resource;
            }

            Debug.LogError($"无法加载资源: {path}, 错误: {handle.OperationException}");
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
            return null;
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        public void LoadAsync<T>(
            string path,
            Action<T> onComplete = null,
            Action<Exception> onFail = null) where T : UnityEngine.Object
        {
            string cacheKey = MakeCacheKey<T>(path);

            if (_resourceCache.TryGetResource<T>(cacheKey, out T cachedResource))
            {
                AddReference(cacheKey);
                onComplete?.Invoke(cachedResource);
                return;
            }

            // 缓存未命中但残留句柄（缓存层曾清掉失效对象），先释放再加载
            ReleaseStaleHandleIfNeeded(cacheKey);

            var pendingCallback = new PendingLoadCallback
            {
                OnComplete = resource => onComplete?.Invoke(resource as T),
                OnFail = onFail
            };

            if (_pendingLoadRequests.TryGetValue(cacheKey, out var pendingList))
            {
                pendingList.Add(pendingCallback);
                return;
            }

            _pendingLoadRequests[cacheKey] = new List<PendingLoadCallback> { pendingCallback };

            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(path);
            _loadingHandles[cacheKey] = handle;

            handle.Completed += op =>
            {
                // ClearAll 已提前释放
                if (!_loadingHandles.Remove(cacheKey))
                {
                    if (op.IsValid())
                    {
                        Addressables.Release(op);
                    }
                    return;
                }

                List<PendingLoadCallback> pendingCallbacks = TakePendingCallbacks(cacheKey);

                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    T resource = op.Result;
                    _addressableHandles[cacheKey] = new AddressableResourceHandle<T>(op);
                    _referenceCounts[cacheKey] = Math.Max(1, pendingCallbacks.Count);
                    _resourceCache.AddResource(cacheKey, resource);

                    foreach (var callback in pendingCallbacks)
                    {
                        callback.OnComplete?.Invoke(resource);
                    }
                }
                else
                {
                    Debug.LogError($"Addressables加载资源失败: {path}, 错误: {op.OperationException}");

                    if (op.IsValid())
                    {
                        Addressables.Release(op);
                    }

                    var exception = op.OperationException ?? new Exception($"Addressables加载资源失败: {path}");
                    NotifyLoadFailed(pendingCallbacks, exception);
                }
            };
        }

        /// <summary>
        /// 释放资源引用（该地址下所有类型各减一）
        /// </summary>
        /// <param name="path">资源路径</param>
        public void Release(string path)
        {
            List<string> keys = FindCacheKeysForPath(path);
            if (keys.Count == 0)
            {
                return;
            }

            foreach (string cacheKey in keys)
            {
                ReleaseByCacheKey(cacheKey);
            }
        }

        /// <summary>
        /// 释放指定类型的资源引用
        /// </summary>
        public void Release<T>(string path) where T : UnityEngine.Object
        {
            ReleaseByCacheKey(MakeCacheKey<T>(path));
        }

        private void ReleaseByCacheKey(string cacheKey)
        {
            if (_referenceCounts.TryGetValue(cacheKey, out int refCount))
            {
                refCount--;
                if (refCount <= 0)
                {
                    ReleaseInternal(cacheKey);
                }
                else
                {
                    _referenceCounts[cacheKey] = refCount;
                }
            }
            // 无引用计数但残留句柄时强制释放，避免泄漏
            else if (_addressableHandles.ContainsKey(cacheKey))
            {
                ReleaseInternal(cacheKey);
            }
        }

        /// <summary>
        /// 增加资源引用计数（缓存命中或复用时）
        /// </summary>
        private void AddReference(string cacheKey)
        {
            if (_referenceCounts.TryGetValue(cacheKey, out int count))
            {
                _referenceCounts[cacheKey] = count + 1;
            }
            else
            {
                // 缓存与引用计数不同步时兜底，避免 KeyNotFoundException
                _referenceCounts[cacheKey] = 1;
            }
        }

        /// <summary>
        /// 取出并移除指定缓存键的待处理回调列表
        /// </summary>
        private List<PendingLoadCallback> TakePendingCallbacks(string cacheKey)
        {
            if (_pendingLoadRequests.TryGetValue(cacheKey, out var pendingCallbacks))
            {
                _pendingLoadRequests.Remove(cacheKey);
                return pendingCallbacks;
            }

            return new List<PendingLoadCallback>();
        }

        /// <summary>
        /// 通知所有等待方加载失败：优先 OnFail；未提供时才回退 OnComplete(null)，避免双重回调
        /// </summary>
        private static void NotifyLoadFailed(List<PendingLoadCallback> pendingCallbacks, Exception exception)
        {
            foreach (var callback in pendingCallbacks)
            {
                if (callback.OnFail != null)
                {
                    callback.OnFail.Invoke(exception);
                }
                else
                {
                    callback.OnComplete?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// 缓存未命中时，若仍残留 Addressables 句柄则强制释放，避免重复加载覆盖导致泄漏
        /// </summary>
        private void ReleaseStaleHandleIfNeeded(string cacheKey)
        {
            if (_addressableHandles.ContainsKey(cacheKey))
            {
                Debug.LogWarning(
                    $"[ResourceManager] 检测到残留句柄（缓存已失效），强制释放: {cacheKey.Replace(CacheKeySeparator, '|')}");
                ReleaseInternal(cacheKey);
            }
        }

        /// <summary>
        /// 内部资源释放方法（仅 Addressables）
        /// </summary>
        private void ReleaseInternal(string cacheKey)
        {
            if (_addressableHandles.TryGetValue(cacheKey, out var handle))
            {
                handle.Release();
                _addressableHandles.Remove(cacheKey);
            }
            else
            {
                Debug.LogWarning($"[ResourceManager] ReleaseInternal: 未找到 Addressables 句柄: {cacheKey.Replace(CacheKeySeparator, '|')}");
            }

            _referenceCounts.Remove(cacheKey);
            _resourceCache.RemoveResource(cacheKey);
        }

        /// <summary>
        /// 强制释放全部资源：取消预加载与进行中请求，Release 所有 Addressables 句柄并清空缓存。
        /// </summary>
        public void ClearAll()
        {
            CancelAllPreloads();

            List<string> pendingKeys = _pendingLoadRequests.Keys.ToList();
            var clearException = new OperationCanceledException("ResourceManager.ClearAll");
            foreach (string cacheKey in pendingKeys)
            {
                List<PendingLoadCallback> callbacks = TakePendingCallbacks(cacheKey);
                NotifyLoadFailed(callbacks, clearException);
            }

            foreach (var loadingHandle in _loadingHandles.Values)
            {
                if (loadingHandle.IsValid())
                {
                    Addressables.Release(loadingHandle);
                }
            }
            _loadingHandles.Clear();

            foreach (var handle in _addressableHandles.Values)
            {
                handle.Release();
            }
            _addressableHandles.Clear();
            _referenceCounts.Clear();
            _resourceCache.Clear();
            _preloadTasks.Clear();

            Debug.Log("[ResourceManager] ClearAll 完成");
        }

        /// <summary>
        /// 卸载未使用的资源：释放引用计数归零的 Addressables 句柄，并清理无效句柄。
        /// </summary>
        public void UnloadUnusedAssets()
        {
            List<string> keysToRemove = new List<string>();
            foreach (var kvp in _referenceCounts)
            {
                if (kvp.Value <= 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (string key in keysToRemove)
            {
                ReleaseInternal(key);
            }

            List<string> invalidHandles = new List<string>();
            foreach (var kvp in _addressableHandles)
            {
                if (!kvp.Value.IsValid)
                {
                    invalidHandles.Add(kvp.Key);
                }
            }

            foreach (string key in invalidHandles)
            {
                _addressableHandles.Remove(key);
                _referenceCounts.Remove(key);
                _resourceCache.RemoveResource(key);
            }
        }

        /// <summary>
        /// 获取资源的引用计数（该地址下所有类型合计）
        /// </summary>
        public int GetReferenceCount(string path)
        {
            int total = 0;
            foreach (string cacheKey in FindCacheKeysForPath(path))
            {
                if (_referenceCounts.TryGetValue(cacheKey, out int count))
                {
                    total += count;
                }
            }
            return total;
        }

        /// <summary>
        /// 获取指定类型资源的引用计数
        /// </summary>
        public int GetReferenceCount<T>(string path) where T : UnityEngine.Object
        {
            return _referenceCounts.TryGetValue(MakeCacheKey<T>(path), out int count) ? count : 0;
        }

        /// <summary>
        /// 获取当前缓存的资源数量
        /// </summary>
        public int GetCachedResourceCount()
        {
            return _resourceCache.GetCachedResourceCount();
        }

        /// <summary>
        /// 验证所有缓存资源的有效性；无效项同步释放 Addressables 句柄
        /// </summary>
        public int ValidateAndCleanAllResources()
        {
            List<string> keysToRelease = new List<string>();

            foreach (string cacheKey in _addressableHandles.Keys)
            {
                if (!_resourceCache.IsResourceValid(cacheKey))
                {
                    keysToRelease.Add(cacheKey);
                }
            }

            foreach (string cacheKey in keysToRelease)
            {
                ReleaseInternal(cacheKey);
            }

            // 清理无句柄的残留缓存条目
            return keysToRelease.Count + _resourceCache.CleanInvalidResources();
        }

        /// <summary>
        /// 获取资源管理器的内存使用概览
        /// </summary>
        public ResourceManagerStats GetStats()
        {
            return new ResourceManagerStats
            {
                TotalCachedResources = GetCachedResourceCount(),
                TrackedReferenceCounts = _referenceCounts.Count
            };
        }

        #region 资源预加载

        private readonly Dictionary<string, PreloadTaskInfo> _preloadTasks = new Dictionary<string, PreloadTaskInfo>();
        private int _maxConcurrentPreloads = 5;
        private CancellationTokenSource _preloadCts;
        private static MethodInfo _preloadResourcesAsyncGenericMethod;

        /// <summary>
        /// WebGL 安全的异步并发闸门。
        /// 不可使用 SemaphoreSlim.WaitAsync：WebGL 无 ThreadPool，排队等待会永久卡死。
        /// </summary>
        private sealed class AsyncConcurrencyGate
        {
            private readonly int _maxCount;
            private int _currentCount;
            private readonly Queue<UniTaskCompletionSource> _waitQueue = new Queue<UniTaskCompletionSource>();

            public AsyncConcurrencyGate(int maxCount)
            {
                _maxCount = Math.Max(1, maxCount);
            }

            public async UniTask WaitAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_currentCount < _maxCount)
                {
                    _currentCount++;
                    return;
                }

                var tcs = new UniTaskCompletionSource();
                _waitQueue.Enqueue(tcs);

                CancellationTokenRegistration registration = default;
                if (cancellationToken.CanBeCanceled)
                {
                    registration = cancellationToken.Register(
                        state => ((UniTaskCompletionSource)state).TrySetCanceled(),
                        tcs);
                }

                try
                {
                    await tcs.Task;
                }
                finally
                {
                    registration.Dispose();
                }
            }

            public void Release()
            {
                while (_waitQueue.Count > 0)
                {
                    var next = _waitQueue.Dequeue();
                    // 已取消的 waiter 跳过，槽位转给下一个真正等待的任务
                    if (next.TrySetResult())
                    {
                        return;
                    }
                }

                if (_currentCount > 0)
                {
                    _currentCount--;
                }
            }
        }

        /// <summary>
        /// 批量预加载资源（按运行时类型，异步，并发限流 + WhenAll）
        /// </summary>
        public UniTask PreloadResourcesAsync(
            string[] paths,
            Type assetType,
            Action<float> onProgress = null,
            Action onComplete = null,
            CancellationToken cancellationToken = default)
        {
            if (assetType == null)
            {
                assetType = typeof(UnityEngine.Object);
            }

            if (!typeof(UnityEngine.Object).IsAssignableFrom(assetType))
            {
                Debug.LogError($"[ResourceManager] 预加载类型无效: {assetType}");
                onComplete?.Invoke();
                return UniTask.CompletedTask;
            }

            if (_preloadResourcesAsyncGenericMethod == null)
            {
                _preloadResourcesAsyncGenericMethod = typeof(ResourceManager)
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .First(m => m.Name == nameof(PreloadResourcesAsync) && m.IsGenericMethodDefinition);
            }

            var genericMethod = _preloadResourcesAsyncGenericMethod.MakeGenericMethod(assetType);
            return (UniTask)genericMethod.Invoke(
                this,
                new object[] { paths, onProgress, onComplete, cancellationToken });
        }

        /// <summary>
        /// 批量预加载资源（异步，并发限流 + WhenAll）
        /// </summary>
        public async UniTask PreloadResourcesAsync<T>(
            string[] paths,
            Action<float> onProgress = null,
            Action onComplete = null,
            CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            CancelAllPreloads();

            if (paths == null || paths.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            _preloadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            CancellationToken ct = _preloadCts.Token;

            _preloadTasks.Clear();

            string[] uniquePaths = paths
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .ToArray();

            if (uniquePaths.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            foreach (string path in uniquePaths)
            {
                _preloadTasks[path] = new PreloadTaskInfo
                {
                    ResourcePath = path,
                    Status = PreloadTaskStatus.Pending
                };
            }

            int total = uniquePaths.Length;
            int finished = 0;
            Debug.Log($"开始预加载 {total} 个资源，最大并发数: {_maxConcurrentPreloads}");

            var gate = new AsyncConcurrencyGate(_maxConcurrentPreloads);
            UniTask[] tasks = uniquePaths.Select(path => PreloadOneAsync<T>(
                path,
                gate,
                ct,
                () =>
                {
                    int done = Interlocked.Increment(ref finished);
                    onProgress?.Invoke((float)done / total);
                })).ToArray();

            try
            {
                await UniTask.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("预加载已取消");
            }

            int completedTasks = _preloadTasks.Values.Count(t => t.Status == PreloadTaskStatus.Completed);
            int failedTasks = _preloadTasks.Values.Count(t => t.Status == PreloadTaskStatus.Failed);
            Debug.Log($"预加载完成: 成功 {completedTasks}/{total}, 失败 {failedTasks}");

            if (failedTasks > 0 && !ct.IsCancellationRequested)
            {
                Debug.LogWarning("部分资源预加载失败，请检查日志获取详细信息");
            }

            onComplete?.Invoke();
        }

        private async UniTask PreloadOneAsync<T>(
            string path,
            AsyncConcurrencyGate gate,
            CancellationToken ct,
            Action onFinished) where T : UnityEngine.Object
        {
            try
            {
                await gate.WaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                if (_preloadTasks.TryGetValue(path, out var cancelledInfo))
                {
                    cancelledInfo.Status = PreloadTaskStatus.Failed;
                    cancelledInfo.Error = new OperationCanceledException("预加载任务被取消");
                }
                onFinished?.Invoke();
                return;
            }

            try
            {
                ct.ThrowIfCancellationRequested();

                if (_preloadTasks.TryGetValue(path, out var info))
                {
                    info.Status = PreloadTaskStatus.Loading;
                }

                await LoadAssetAsync<T>(path);

                if (_preloadTasks.TryGetValue(path, out info))
                {
                    info.Status = PreloadTaskStatus.Completed;
                }
            }
            catch (OperationCanceledException)
            {
                if (_preloadTasks.TryGetValue(path, out var info))
                {
                    info.Status = PreloadTaskStatus.Failed;
                    info.Error = new OperationCanceledException("预加载任务被取消");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"预加载资源失败: {path}, 错误: {ex.Message}");
                if (_preloadTasks.TryGetValue(path, out var info))
                {
                    info.Status = PreloadTaskStatus.Failed;
                    info.Error = ex;
                }
            }
            finally
            {
                gate.Release();
                onFinished?.Invoke();
            }
        }

        /// <summary>
        /// 异步加载资源并返回结果
        /// </summary>
        public async UniTask<T> LoadAssetAsync<T>(string path) where T : UnityEngine.Object
        {
            var tcs = new UniTaskCompletionSource<T>();
            LoadAsync<T>(
                path,
                resource =>
                {
                    if (resource != null)
                    {
                        tcs.TrySetResult(resource);
                    }
                    else
                    {
                        tcs.TrySetException(new Exception($"资源加载返回空: {path}"));
                    }
                },
                exception =>
                {
                    tcs.TrySetException(exception ?? new Exception($"资源加载失败: {path}"));
                });

            return await tcs.Task;
        }

        /// <summary>
        /// 取消批量预加载调度（已发起的 LoadAsync 仍会完成并入库）
        /// </summary>
        public void CancelAllPreloads()
        {
            if (_preloadCts != null)
            {
                try
                {
                    if (!_preloadCts.IsCancellationRequested)
                    {
                        _preloadCts.Cancel();
                    }
                }
                catch (ObjectDisposedException)
                {
                }

                _preloadCts.Dispose();
                _preloadCts = null;
            }

            foreach (var task in _preloadTasks.Values)
            {
                if (task.Status == PreloadTaskStatus.Loading || task.Status == PreloadTaskStatus.Pending)
                {
                    task.Status = PreloadTaskStatus.Failed;
                    task.Error = new OperationCanceledException("预加载任务被取消");
                }
            }
        }

        #endregion
    }
}
