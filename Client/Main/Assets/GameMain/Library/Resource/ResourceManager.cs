using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks; // 引入 UniTask
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
        /// <summary>
        /// 总缓存资源数
        /// </summary>
        public int TotalCachedResources { get; set; }

        /// <summary>
        /// 跟踪的引用计数数
        /// </summary>
        public int TrackedReferenceCounts { get; set; }

        /// <summary>
        /// 路径映射数
        /// </summary>
        public int PathMappings { get; set; }

        /// <summary>
        /// 当前使用的加载策略
        /// </summary>
        public string CurrentStrategy { get; set; }

        /// <summary>
        /// 预加载任务总数
        /// </summary>
        public int TotalPreloadTasks { get; set; }

        /// <summary>
        /// 已完成的预加载任务数
        /// </summary>
        public int CompletedPreloadTasks { get; set; }

        public override string ToString()
        {
            return $"总缓存资源: {TotalCachedResources}, 引用计数: {TrackedReferenceCounts}, 路径映射: {PathMappings}, 策略: {CurrentStrategy}, 预加载进度: {CompletedPreloadTasks}/{TotalPreloadTasks}";
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
        public float Progress { get; set; }
        public object ResourceHandle { get; set; } // 存储资源句柄或引用
    }

    /// <summary>
    /// 资源管理器，用于统一管理游戏中的各种资源加载、卸载和缓存
    /// 设计上考虑了未来向AssetBundle或Addressables迁移的可能性
    /// </summary>
    public class ResourceManager
    {
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

        // 引用计数，用于跟踪资源使用情况
        private readonly Dictionary<string, int> _referenceCounts = new Dictionary<string, int>();

        // 资源加载路径映射，便于未来迁移
        private readonly Dictionary<string, string> _pathMapping = new Dictionary<string, string>();

        // 资源加载策略
        private IResourceLoadStrategy _loadStrategy;

        // 资源缓存实例
        private IResourceCache _resourceCache;

        // 存储正在进行中的异步加载请求，防止重复加载同一资源
        private readonly Dictionary<string, List<Action<UnityEngine.Object>>> _pendingLoadRequests = new Dictionary<string, List<Action<UnityEngine.Object>>>();

        // Addressables资源句柄字典，用于正确释放Addressables资源
        private readonly Dictionary<string, IReleasableResource> _addressableHandles = new Dictionary<string, IReleasableResource>();

        // 私有构造函数防止外部实例化
        public void Initialize()
        {
            SpriteLoader = SpriteLoader.Instance;
            SpriteLoader.Init();
            // 默认使用Resources加载策略
            _loadStrategy = new AddressablesLoadStrategy();
            _resourceCache = new ResourceCache();
        }

        /// <summary>
        /// 设置资源加载策略
        /// </summary>
        /// <param name="strategy">资源加载策略</param>
        public void SetLoadStrategy(IResourceLoadStrategy strategy)
        {
            _loadStrategy = strategy;
        }

        /// <summary>
        /// 加载资源（同步方式）
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径</param>
        /// <returns>加载的资源</returns>
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            string actualPath = GetActualPath(path);

            // 检查缓存中是否已存在且有效
            if (_resourceCache.TryGetResource<T>(actualPath, out T cachedResource))
            {
                _referenceCounts[actualPath]++;
                return cachedResource;
            }

            // 实际加载资源
            T resource = _loadStrategy.Load<T>(actualPath);
            if (resource != null)
            {
                // 添加到缓存
                _resourceCache.AddResource<T>(actualPath, resource);
                _referenceCounts[actualPath] = 1;
            }
            else
            {
                Debug.LogError($"无法加载资源: {actualPath}");
            }

            return resource;
        }

        /// <summary>
        /// 预加载资源（同步方式）
        /// </summary>
        public ResourceRequest PreLoadRes<T>(string path) where T : UnityEngine.Object
        {
            string actualPath = GetActualPath(path);

            // 检查缓存中是否已存在且有效
            if (_resourceCache.TryGetResource<T>(actualPath, out T cachedResource))
            {
                return null;
            }

            return LoadAsync<T>(actualPath, null);
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径</param>
        /// <param name="onComplete">加载完成回调</param>
        /// <returns>资源加载请求</returns>
        public ResourceRequest LoadAsync<T>(string path, Action<T> onComplete = null, Action<Exception> onFail = null) where T : UnityEngine.Object
        {
            string actualPath = GetActualPath(path);

            // 检查缓存中是否已存在且有效
            if (_resourceCache.TryGetResource<T>(actualPath, out T cachedResource))
            {
                _referenceCounts[actualPath]++;
                onComplete?.Invoke(cachedResource);
                return null; // 返回null表示已从缓存加载
            }

            // 检查是否已经有相同的资源正在加载中
            if (_pendingLoadRequests.ContainsKey(actualPath))
            {
                // 如果有，则将回调添加到待处理列表中，避免重复加载
                _pendingLoadRequests[actualPath].Add((resource) => onComplete?.Invoke(resource as T));
                return null;
            }

            // 创建待处理回调列表并添加当前回调
            _pendingLoadRequests[actualPath] = new List<Action<UnityEngine.Object>> { (resource) => onComplete?.Invoke(resource as T) };

            // 如果使用Addressables策略，特殊处理
            if (_loadStrategy is AddressablesLoadStrategy)
            {
                AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(actualPath);
                handle.Completed += (op) =>
                {
                    // 获取所有等待此资源加载完成的回调
                    List<Action<UnityEngine.Object>> pendingCallbacks = null;
                    if (_pendingLoadRequests.ContainsKey(actualPath))
                    {
                        pendingCallbacks = _pendingLoadRequests[actualPath];
                        _pendingLoadRequests.Remove(actualPath);
                    }

                    if (op.Status == AsyncOperationStatus.Succeeded)
                    {
                        T resource = op.Result;

                        // 保存句柄以便后续释放
                        _addressableHandles[actualPath] = new AddressableResourceHandle<T>(op);
                        _referenceCounts[actualPath] = 1;

                        // *** 添加这行关键代码 ***
                        _resourceCache.AddResource<T>(actualPath, resource);

                        // 执行所有等待的回调
                        if (pendingCallbacks != null)
                        {
                            foreach (var callback in pendingCallbacks)
                            {
                                callback?.Invoke(resource);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError($"Addressables加载资源失败: {actualPath}, 错误: {op.OperationException}");
                        onFail?.Invoke(op.OperationException);
                        // 执行所有等待的回调，传入null
                        if (pendingCallbacks != null)
                        {
                            foreach (var callback in pendingCallbacks)
                            {
                                callback?.Invoke(null);
                            }
                        }
                    }
                };

                return null;
            }

            // 异步加载资源 (Resources策略)
            ResourceRequest request = _loadStrategy.LoadAsync<T>(actualPath, (resource) =>
            {
                // 获取所有等待此资源加载完成的回调
                List<Action<UnityEngine.Object>> pendingCallbacks = null;
                if (_pendingLoadRequests.ContainsKey(actualPath))
                {
                    pendingCallbacks = _pendingLoadRequests[actualPath];
                    _pendingLoadRequests.Remove(actualPath);
                }

                if (resource != null)
                {
                    // 添加到缓存
                    _resourceCache.AddResource<T>(actualPath, resource);
                    _referenceCounts[actualPath] = 1;
                }
                else
                {
                    Debug.LogError($"无法异步加载资源: {actualPath}");
                }

                // 执行所有等待的回调
                if (pendingCallbacks != null)
                {
                    foreach (var callback in pendingCallbacks)
                    {
                        callback?.Invoke(resource);
                    }
                }
            });

            return request;
        }

        /// <summary>
        /// 释放资源引用
        /// </summary>
        /// <param name="path">资源路径</param>
        public void Release(string path)
        {
            string actualPath = GetActualPath(path);

            if (_referenceCounts.ContainsKey(actualPath))
            {
                _referenceCounts[actualPath]--;

                // 当引用计数为0时，释放资源
                if (_referenceCounts[actualPath] <= 0)
                {
                    ReleaseInternal(actualPath);
                }
            }

            // 特殊处理Addressables资源
            else if (_addressableHandles.ContainsKey(actualPath))
            {
                ReleaseInternal(actualPath);
            }
        }

        /// <summary>
        /// 内部资源释放方法
        /// </summary>
        /// <param name="path">资源路径</param>
        private void ReleaseInternal(string path)
        {
            // 如果是Addressables资源，使用Addressables的方式释放
            if (_addressableHandles.ContainsKey(path))
            {
                var handle = _addressableHandles[path];
                handle.Release();
                _addressableHandles.Remove(path);
                _referenceCounts.Remove(path);
                return;
            }

            // 其他资源使用原有方式处理
            if (_referenceCounts.ContainsKey(path))
            {
                _referenceCounts.Remove(path);
                // 不主动从缓存中移除，因为其他地方可能还在使用
                // 真正的卸载将通过UnloadUnusedAssets或系统自动完成
            }
        }

        /// <summary>
        /// 卸载未使用的资源（强制释放）
        /// 类似于Resources.UnloadUnusedAssets()，但在ResourceManager层面进行更精细的控制
        /// </summary>
        public void UnloadUnusedAssets()
        {
            // 清理引用计数为0的资源
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
                _resourceCache.RemoveResource(key);
            }

            // 清理无效的Addressables资源句柄
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
            }

            // 调用Unity的资源卸载机制
            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// 添加路径映射（为未来迁移做准备）
        /// </summary>
        /// <param name="logicalPath">逻辑路径</param>
        /// <param name="physicalPath">物理路径</param>
        public void AddPathMapping(string logicalPath, string physicalPath)
        {
            _pathMapping[logicalPath] = physicalPath;
        }

        /// <summary>
        /// 获取实际资源路径
        /// </summary>
        /// <param name="logicalPath">逻辑路径</param>
        /// <returns>实际路径</returns>
        private string GetActualPath(string logicalPath)
        {
            return _pathMapping.ContainsKey(logicalPath) ? _pathMapping[logicalPath] : logicalPath;
        }

        /// <summary>
        /// 预加载一组资源
        /// </summary>
        /// <param name="paths">资源路径数组</param>
        public void PreloadResources(string[] paths)
        {
            foreach (string path in paths)
            {
                Load<UnityEngine.Object>(path);
            }
        }

        /// <summary>
        /// 获取资源的引用计数
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns>引用计数</returns>
        public int GetReferenceCount(string path)
        {
            string actualPath = GetActualPath(path);
            return _referenceCounts.ContainsKey(actualPath) ? _referenceCounts[actualPath] : 0;
        }

        /// <summary>
        /// 获取当前缓存的资源数量
        /// </summary>
        /// <returns>缓存资源数量</returns>
        public int GetCachedResourceCount()
        {
            return _resourceCache.GetCachedResourceCount();
        }

        /// <summary>
        /// 验证所有缓存资源的有效性并清理无效资源
        /// </summary>
        /// <returns>清理的资源数量</returns>
        public int ValidateAndCleanAllResources()
        {
            return _resourceCache.CleanInvalidResources();
        }

        /// <summary>
        /// 获取资源管理器的内存使用概览
        /// </summary>
        /// <returns>内存使用信息</returns>
        public ResourceManagerStats GetStats()
        {
            return new ResourceManagerStats
            {
                TotalCachedResources = GetCachedResourceCount(),
                TrackedReferenceCounts = _referenceCounts.Count,
                PathMappings = _pathMapping.Count,
                CurrentStrategy = _loadStrategy.GetType().Name
            };
        }

        #region 资源预加载
        // 预加载任务管理
        private readonly Dictionary<string, PreloadTaskInfo> _preloadTasks = new Dictionary<string, PreloadTaskInfo>();
        private readonly Queue<string> _preloadQueue = new Queue<string>();
        private int _maxConcurrentPreloads = 5; // 最大并发预加载数
        private int _currentPreloadCount = 0;


        /// <summary>
        /// 设置最大并发预加载数量
        /// </summary>
        /// <param name="maxCount">最大并发数</param>
        public void SetMaxConcurrentPreloads(int maxCount)
        {
            _maxConcurrentPreloads = Math.Max(1, maxCount);
        }

        /// <summary>
        /// 获取预加载任务信息
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns>预加载任务信息</returns>
        public PreloadTaskInfo GetPreloadTaskInfo(string path)
        {
            string actualPath = GetActualPath(path);
            return _preloadTasks.ContainsKey(actualPath) ? _preloadTasks[actualPath] : null;
        }

        /// <summary>
        /// 获取所有预加载任务的总体进度
        /// </summary>
        /// <returns>总体进度(0-1)</returns>
        public float GetOverallPreloadProgress()
        {
            if (_preloadTasks.Count == 0) return 1f;

            float totalProgress = 0f;
            foreach (var task in _preloadTasks.Values)
            {
                totalProgress += task.Progress;
            }
            return totalProgress / _preloadTasks.Count;
        }

        /// <summary>
        /// 批量预加载资源（异步）
        /// </summary>
        /// <param name="paths">资源路径数组</param>
        /// <param name="onProgress">进度回调</param>
        /// <param name="onComplete">完成回调</param>
        /// <returns>UniTask对象</returns>
        public async UniTask PreloadResourcesAsync<T>(string[] paths, Action<float> onProgress = null, Action onComplete = null) where T : UnityEngine.Object
        {
            if (paths == null || paths.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            // 初始化预加载任务
            foreach (string path in paths)
            {
                string actualPath = GetActualPath(path);
                if (!_preloadTasks.ContainsKey(actualPath))
                {
                    _preloadTasks[actualPath] = new PreloadTaskInfo
                    {
                        ResourcePath = actualPath,
                        Status = PreloadTaskStatus.Pending,
                        Progress = 0f
                    };
                    _preloadQueue.Enqueue(actualPath);
                }
            }

            int totalTasks = _preloadQueue.Count;
            Debug.Log($"开始预加载 {totalTasks} 个资源，最大并发数: {_maxConcurrentPreloads}");

            // 持续处理预加载任务直到队列为空
            while (_preloadQueue.Count > 0 || _currentPreloadCount > 0)
            {
                // 启动新的预加载任务直到达到最大并发数
                while (_currentPreloadCount < _maxConcurrentPreloads && _preloadQueue.Count > 0)
                {
                    string path = _preloadQueue.Dequeue();
                    _currentPreloadCount++;
                    
                    Debug.Log($"启动预加载任务: {path} (当前并发: {_currentPreloadCount})");

                    // 使用 UniTask 替代 Task.Run
                    UniTask preloadTask = UniTask.Create(async () =>
                    {
                        try
                        {
                            await PreloadSingleResourceAsync<T>(path, (progress) =>
                            {
                                // 更新单个任务进度
                                if (_preloadTasks.ContainsKey(path))
                                {
                                    _preloadTasks[path].Progress = progress;
                                }

                                // 计算整体进度
                                float overallProgress = GetOverallPreloadProgress();
                                onProgress?.Invoke(overallProgress);
                            });

                            // 任务成功完成
                            if (_preloadTasks.ContainsKey(path))
                            {
                                _preloadTasks[path].Status = PreloadTaskStatus.Completed;
                                _preloadTasks[path].Progress = 1f;
                            }
                            
                            Debug.Log($"预加载任务完成: {path}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"预加载资源失败: {path}, 错误: {ex.Message}");
                            if (_preloadTasks.ContainsKey(path))
                            {
                                _preloadTasks[path].Status = PreloadTaskStatus.Failed;
                                _preloadTasks[path].Error = ex;
                            }
                        }
                        finally
                        {
                            _currentPreloadCount--;
                            Debug.Log($"任务结束: {path} (当前并发: {_currentPreloadCount})");
                        }
                    });

                    // 不需要等待，让任务并发执行
                }

                // 等待一帧再检查是否有新任务可以启动
                await UniTask.DelayFrame(1);
                
                // 更新总体进度
                float currentProgress = GetOverallPreloadProgress();
                Debug.Log($"当前总体进度: {currentProgress:P0} ({_currentPreloadCount} 个任务进行中)");
            }

            // 验证所有任务都已完成
            int completedTasks = _preloadTasks.Values.Count(t => t.Status == PreloadTaskStatus.Completed);
            int failedTasks = _preloadTasks.Values.Count(t => t.Status == PreloadTaskStatus.Failed);
            
            Debug.Log($"预加载完成: 成功 {completedTasks}/{totalTasks}, 失败 {failedTasks}");
            
            if (failedTasks > 0)
            {
                Debug.LogWarning("部分资源预加载失败，请检查日志获取详细信息");
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// 预加载单个资源（异步）
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="onProgress">进度回调</param>
        /// <returns>UniTask对象</returns>
        private async UniTask PreloadSingleResourceAsync<T>(string path, Action<float> onProgress) where T : UnityEngine.Object
        {
            string actualPath = GetActualPath(path);

            // 检查是否已在缓存中
            if (_resourceCache.TryGetResource<T>(actualPath, out var cachedResource))
            {
                _referenceCounts[actualPath]++;
                onProgress?.Invoke(1f);
                return;
            }

            // 更新任务状态
            if (_preloadTasks.ContainsKey(actualPath))
            {
                _preloadTasks[actualPath].Status = PreloadTaskStatus.Loading;
            }

            // 执行实际的异步加载
            UniTaskCompletionSource<bool> tcs = new UniTaskCompletionSource<bool>();

            _ = LoadAsync<T>(actualPath,
                (resource) =>
                {
                    onProgress?.Invoke(1f);
                    tcs.TrySetResult(true);
                },
                (exception) =>
                {
                    onProgress?.Invoke(1f);
                    tcs.TrySetException(exception);
                });

            await tcs.Task;
        }

        /// <summary>
        /// 取消所有预加载任务
        /// </summary>
        public void CancelAllPreloads()
        {
            _preloadQueue.Clear();
            _currentPreloadCount = 0;

            // 标记所有任务为失败
            foreach (var task in _preloadTasks.Values)
            {
                if (task.Status == PreloadTaskStatus.Loading || task.Status == PreloadTaskStatus.Pending)
                {
                    task.Status = PreloadTaskStatus.Failed;
                    task.Error = new OperationCanceledException("预加载任务被取消");
                }
            }
        }

        /// <summary>
        /// 获取预加载统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public ResourceManagerStats GetExtendedStats()
        {
            var baseStats = GetStats();
            return new ResourceManagerStats
            {
                TotalCachedResources = baseStats.TotalCachedResources,
                TrackedReferenceCounts = baseStats.TrackedReferenceCounts,
                PathMappings = baseStats.PathMappings,
                CurrentStrategy = baseStats.CurrentStrategy,
                TotalPreloadTasks = _preloadTasks.Count,
                CompletedPreloadTasks = _preloadTasks.Values.Count(t => t.Status == PreloadTaskStatus.Completed)
            };
        }

        #endregion

        #region Internal Debug Support

        /// <summary>
        /// 为调试工具提供内部引用计数访问支持
        /// </summary>
        internal IDictionary<string, int> GetInternalReferenceCounts()
        {
            return _referenceCounts;
        }

        /// <summary>
        /// 为调试工具提供内部资源缓存访问支持
        /// </summary>
        internal IResourceCache GetInternalResourceCache()
        {
            return _resourceCache;
        }

        /// <summary>
        /// 为调试工具提供内部路径映射访问支持
        /// </summary>
        internal IDictionary<string, string> GetInternalPathMappings()
        {
            return _pathMapping;
        }

        #endregion

        public bool CheckResourceValid<T>(string path) where T : UnityEngine.Object
        {
            return _resourceCache.TryGetResource<T>(path, out _);
        }
    }
}