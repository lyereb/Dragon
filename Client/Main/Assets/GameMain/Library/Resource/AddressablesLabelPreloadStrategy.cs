using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Dragon.Resource;

namespace Dragon.Resource
{
    /// <summary>
    /// 基于标签的Addressables预加载策略
    /// </summary>
    public class AddressablesLabelPreloadStrategy
    {
        // 直接使用单例，无需构造函数注入
        private ResourceManager ResourceManager => ResourceManager.Instance;

        /// <summary>
        /// 通过标签预加载资源（使用单例）
        /// </summary>
        public async UniTask PreloadByLabelAsync<T>(
            string label, 
            Action<float> onProgress = null, 
            Action onComplete = null) where T : UnityEngine.Object
        {
            Debug.Log($"[标签预加载策略] 开始预加载标签'{label}'的资源...");

            // 加载标签对应的资源位置
            var locationsHandle = Addressables.LoadResourceLocationsAsync(label, typeof(T));
            await UniTask.WaitUntil(() => locationsHandle.IsDone);

            if (locationsHandle.Status == AsyncOperationStatus.Succeeded)
            {
                List<string> resourceAddresses = new List<string>();
                foreach (var location in locationsHandle.Result)
                {
                    resourceAddresses.Add(location.PrimaryKey);
                    Debug.Log($"[标签预加载策略] 发现资源: {location.PrimaryKey}");
                }

                Debug.Log($"[标签预加载策略] 标签'{label}'共找到 {resourceAddresses.Count} 个资源");

                // 直接使用单例进行预加载
                await ResourceManager.PreloadResourcesAsync<T>(
                    resourceAddresses.ToArray(), 
                    onProgress, 
                    () =>
                    {
                        Debug.Log($"[标签预加载策略] 标签'{label}'预加载完成");
                        onComplete?.Invoke();
                    }
                );

                Addressables.Release(locationsHandle);
            }
            else
            {
                Debug.LogError($"[标签预加载策略] 加载标签'{label}'失败: {locationsHandle.OperationException?.Message}");
                onComplete?.Invoke();
                Addressables.Release(locationsHandle);
            }
        }

        /// <summary>
        /// 预加载多个标签的资源
        /// </summary>
        public async UniTask PreloadMultipleLabelsAsync<T>(
            string[] labels,
            Action<float> onProgress = null,
            Action onComplete = null) where T : UnityEngine.Object
        {
            if (labels == null || labels.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            Debug.Log($"[多标签策略] 开始预加载 {labels.Length} 个标签的资源...");

            float totalProgress = 0f;
            int completedLabels = 0;

            foreach (string label in labels)
            {
                await PreloadByLabelAsync<T>(label, 
                    (progress) =>
                    {
                        // 计算当前标签的进度贡献
                        float labelProgress = (completedLabels + progress) / labels.Length;
                        onProgress?.Invoke(labelProgress);
                    });

                completedLabels++;
                totalProgress = (float)completedLabels / labels.Length;
                onProgress?.Invoke(totalProgress);
            }

            Debug.Log($"[多标签策略] 所有标签预加载完成");
            onComplete?.Invoke();
        }
    }
}