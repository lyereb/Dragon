using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Dragon.Resource
{
    /// <summary>
    /// 基于标签的 Addressables 预加载
    /// </summary>
    public class AddressablesLabelPreloadStrategy
    {
        private ResourceManager ResourceManager => ResourceManager.Instance;

        public async UniTask PreloadByLabelAsync<T>(
            string label,
            Action<float> onProgress = null,
            Action onComplete = null) where T : UnityEngine.Object
        {
            await PreloadByLabelAsync(label, typeof(T), onProgress, onComplete);
        }

        public async UniTask PreloadByLabelAsync(
            string label,
            Type assetType,
            Action<float> onProgress = null,
            Action onComplete = null)
        {
            if (assetType == null)
            {
                assetType = typeof(UnityEngine.Object);
            }

            var locationsHandle = Addressables.LoadResourceLocationsAsync(label, assetType);
            List<string> resourceAddresses;
            try
            {
                await UniTask.WaitUntil(() => !locationsHandle.IsValid() || locationsHandle.IsDone);

                if (!locationsHandle.IsValid() || locationsHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError(
                        $"[标签预加载] 加载标签'{label}'失败: {(locationsHandle.IsValid() ? locationsHandle.OperationException?.Message : "invalid handle")}");
                    onComplete?.Invoke();
                    return;
                }

                resourceAddresses = new List<string>(locationsHandle.Result.Count);
                foreach (var location in locationsHandle.Result)
                {
                    resourceAddresses.Add(location.PrimaryKey);
                }
            }
            finally
            {
                if (locationsHandle.IsValid())
                {
                    Addressables.Release(locationsHandle);
                }
            }

            Debug.Log($"[标签预加载] 标签'{label}'共 {resourceAddresses.Count} 个资源");

            // 按标签声明的真实类型入库，与 LoadAsync<T> 缓存键一致
            await ResourceManager.PreloadResourcesAsync(
                resourceAddresses.ToArray(),
                assetType,
                onProgress,
                onComplete);
        }
    }
}
