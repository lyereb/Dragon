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
            await UniTask.WaitUntil(() => locationsHandle.IsDone);

            if (locationsHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[标签预加载] 加载标签'{label}'失败: {locationsHandle.OperationException?.Message}");
                Addressables.Release(locationsHandle);
                onComplete?.Invoke();
                return;
            }

            var resourceAddresses = new List<string>(locationsHandle.Result.Count);
            foreach (var location in locationsHandle.Result)
            {
                resourceAddresses.Add(location.PrimaryKey);
            }

            Addressables.Release(locationsHandle);

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
