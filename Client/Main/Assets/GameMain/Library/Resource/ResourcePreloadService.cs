using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Dragon.Resource
{
    /// <summary>
    /// 资源预加载服务（单例）
    /// </summary>
    public class ResourcePreloadService
    {
        protected static ResourcePreloadService _instance = null;

        public static ResourcePreloadService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ResourcePreloadService();
                }
                return _instance;
            }
        }

        private readonly AddressablesLabelPreloadStrategy _labelStrategy;

        public ResourcePreloadService()
        {
            _labelStrategy = new AddressablesLabelPreloadStrategy();
        }

        /// <summary>
        /// 启动阶段需要下载的 Addressables key（Config + Bootstrap 标签）
        /// </summary>
        public static object[] GetStartupDownloadKeys()
        {
            var entries = AddressablesLabelNames.BootstrapEntries;
            var keys = new object[entries.Length + 1];
            keys[0] = Dragon.Config.ConfigLoadConst.CONFIG_ADDRESSABLE_LABEL;
            for (int i = 0; i < entries.Length; i++)
            {
                keys[i + 1] = entries[i].Label;
            }
            return keys;
        }

        /// <summary>
        /// 通过标签预加载资源
        /// </summary>
        public async UniTask PreloadByLabelAsync(
            string label,
            Type assetType,
            Action<float> onProgress = null,
            Action onComplete = null)
        {
            await _labelStrategy.PreloadByLabelAsync(label, assetType, onProgress, onComplete);
        }

        /// <summary>
        /// 通过标签预加载资源（泛型）
        /// </summary>
        public async UniTask PreloadByLabelAsync<T>(
            string label,
            Action<float> onProgress = null,
            Action onComplete = null) where T : UnityEngine.Object
        {
            await PreloadByLabelAsync(label, typeof(T), onProgress, onComplete);
        }

        /// <summary>
        /// 预加载启动资源（读取 <see cref="AddressablesLabelNames.BootstrapEntries"/>）
        /// </summary>
        public async UniTask PreloadBootstrapResourcesAsync(Action<float> onProgress = null)
        {
            var entries = AddressablesLabelNames.BootstrapEntries;
            if (entries == null || entries.Length == 0)
            {
                onProgress?.Invoke(1f);
                return;
            }

            int totalSteps = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                int step = i;
                await PreloadByLabelAsync(
                    entries[i].Label,
                    entries[i].AssetType,
                    localProgress => onProgress?.Invoke((step + localProgress) / totalSteps));

                onProgress?.Invoke((float)(i + 1) / totalSteps);
            }

            Debug.Log("[预加载服务] 启动资源预加载完成");
        }
    }
}
