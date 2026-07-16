using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Dragon.Resource
{
    /// <summary>
    /// Addressables 远程资源生命周期：初始化、Catalog 更新、依赖下载。
    /// 当前若未配置 Remote，DownloadSize 为 0，下载步骤会自动跳过。
    /// </summary>
    public class AddressablesRemoteService
    {
        protected static AddressablesRemoteService _instance;

        public static AddressablesRemoteService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AddressablesRemoteService();
                }
                return _instance;
            }
        }

        private bool _initialized;

        /// <summary>
        /// 初始化 Addressables 运行时（可重复调用，仅首次生效）
        /// </summary>
        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_initialized)
            {
                return;
            }

            var handle = Addressables.InitializeAsync();
            await WaitHandleAsync(handle, cancellationToken);

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                throw handle.OperationException
                    ?? new Exception("Addressables 初始化失败");
            }

            // Initialize 句柄按官方建议保留，不 Release
            _initialized = true;
            Debug.Log("[AddressablesRemote] 初始化完成");
        }

        /// <summary>
        /// 检查并更新远程 Catalog。无更新或未配置 Remote Catalog 时直接返回 false。
        /// </summary>
        public async UniTask<bool> UpdateCatalogsAsync(CancellationToken cancellationToken = default)
        {
            await InitializeAsync(cancellationToken);

            var checkHandle = Addressables.CheckForCatalogUpdates(false);
            await WaitHandleAsync(checkHandle, cancellationToken);

            if (checkHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogWarning($"[AddressablesRemote] 检查 Catalog 更新失败: {checkHandle.OperationException?.Message}");
                Addressables.Release(checkHandle);
                return false;
            }

            List<string> catalogs = checkHandle.Result;
            Addressables.Release(checkHandle);

            if (catalogs == null || catalogs.Count == 0)
            {
                Debug.Log("[AddressablesRemote] Catalog 无需更新");
                return false;
            }

            Debug.Log($"[AddressablesRemote] 发现 {catalogs.Count} 个 Catalog 需要更新");

            var updateHandle = Addressables.UpdateCatalogs(catalogs, false);
            await WaitHandleAsync(updateHandle, cancellationToken);

            bool success = updateHandle.Status == AsyncOperationStatus.Succeeded;
            if (!success)
            {
                Debug.LogError($"[AddressablesRemote] Catalog 更新失败: {updateHandle.OperationException?.Message}");
            }
            else
            {
                Debug.Log("[AddressablesRemote] Catalog 更新完成");
            }

            Addressables.Release(updateHandle);
            return success;
        }

        /// <summary>
        /// 查询指定 key（address / label）尚未下载的依赖大小（字节）
        /// </summary>
        public async UniTask<long> GetDownloadSizeAsync(object key, CancellationToken cancellationToken = default)
        {
            await InitializeAsync(cancellationToken);

            var sizeHandle = Addressables.GetDownloadSizeAsync(key);
            await WaitHandleAsync(sizeHandle, cancellationToken);

            if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogWarning($"[AddressablesRemote] 获取下载大小失败 ({key}): {sizeHandle.OperationException?.Message}");
                Addressables.Release(sizeHandle);
                return 0;
            }

            long size = sizeHandle.Result;
            Addressables.Release(sizeHandle);
            return size;
        }

        /// <summary>
        /// 下载指定 key 的依赖。size 为 0 时立即返回。
        /// </summary>
        public async UniTask DownloadDependenciesAsync(
            object key,
            Action<float> onProgress = null,
            CancellationToken cancellationToken = default)
        {
            await InitializeAsync(cancellationToken);

            long size = await GetDownloadSizeAsync(key, cancellationToken);
            if (size <= 0)
            {
                onProgress?.Invoke(1f);
                return;
            }

            Debug.Log($"[AddressablesRemote] 开始下载依赖: {key}, 大小: {FormatBytes(size)}");

            var downloadHandle = Addressables.DownloadDependenciesAsync(key, true);
            try
            {
                while (!downloadHandle.IsDone)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    onProgress?.Invoke(downloadHandle.PercentComplete);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                if (downloadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw downloadHandle.OperationException
                        ?? new Exception($"下载依赖失败: {key}");
                }

                onProgress?.Invoke(1f);
                Debug.Log($"[AddressablesRemote] 下载完成: {key}");
            }
            finally
            {
                if (downloadHandle.IsValid())
                {
                    Addressables.Release(downloadHandle);
                }
            }
        }

        /// <summary>
        /// 启动资源准备：初始化 + Catalog 更新 + 按 keys 下载依赖。
        /// 全部为本地资源时会快速跳过下载。
        /// </summary>
        public async UniTask PrepareStartupResourcesAsync(
            IReadOnlyList<object> keys,
            Action<float> onProgress = null,
            CancellationToken cancellationToken = default)
        {
            await InitializeAsync(cancellationToken);
            await UpdateCatalogsAsync(cancellationToken);

            if (keys == null || keys.Count == 0)
            {
                onProgress?.Invoke(1f);
                return;
            }

            var sizes = new long[keys.Count];
            long totalSize = 0;

            for (int i = 0; i < keys.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                sizes[i] = await GetDownloadSizeAsync(keys[i], cancellationToken);
                totalSize += Math.Max(0, sizes[i]);
            }

            if (totalSize <= 0)
            {
                Debug.Log("[AddressablesRemote] 启动资源均为本地或已缓存，跳过下载");
                onProgress?.Invoke(1f);
                return;
            }

            Debug.Log($"[AddressablesRemote] 启动资源需下载: {FormatBytes(totalSize)}");

            long finishedBytes = 0;
            for (int i = 0; i < keys.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (sizes[i] <= 0)
                {
                    continue;
                }

                long keySize = sizes[i];
                await DownloadDependenciesAsync(
                    keys[i],
                    progress =>
                    {
                        long current = finishedBytes + (long)(keySize * Mathf.Clamp01(progress));
                        onProgress?.Invoke((float)current / totalSize);
                    },
                    cancellationToken);

                finishedBytes += keySize;
                onProgress?.Invoke((float)finishedBytes / totalSize);
            }

            onProgress?.Invoke(1f);
        }

        private static async UniTask WaitHandleAsync(AsyncOperationHandle handle, CancellationToken cancellationToken)
        {
            await UniTask.WaitUntil(() => handle.IsDone, cancellationToken: cancellationToken);
        }

        private static async UniTask WaitHandleAsync<T>(AsyncOperationHandle<T> handle, CancellationToken cancellationToken)
        {
            await UniTask.WaitUntil(() => handle.IsDone, cancellationToken: cancellationToken);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
            return $"{bytes / (1024f * 1024f):F2} MB";
        }
    }
}
