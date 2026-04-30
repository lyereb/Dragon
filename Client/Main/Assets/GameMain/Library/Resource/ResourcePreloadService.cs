using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Dragon.Resource
{
    /// <summary>
    /// 资源预加载服务（使用单例模式）
    /// </summary>
    public class ResourcePreloadService
    {
        #region //静态变量

        //静态变量
        protected static ResourcePreloadService _instance = null;

        //静态属性
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
        #endregion

        private readonly AddressablesLabelPreloadStrategy _labelStrategy;


        public ResourcePreloadService()
        {
            // 无参构造函数，策略类内部使用单例

            _labelStrategy = new AddressablesLabelPreloadStrategy();

        }

        /// <summary>
        /// 通过标签预加载资源
        /// </summary>
        public async UniTask PreloadByLabelAsync<T>(
            string label,
            Action<float> onProgress = null,
            Action onComplete = null) where T : UnityEngine.Object
        {
            await _labelStrategy.PreloadByLabelAsync<T>(label, onProgress, onComplete);
        }

        /// <summary>
        /// 预加载启动资源（简化版）
        /// </summary>
        public async UniTask PreloadBootstrapResourcesAsync(Action<float> onProgress = null)
        {
            var bootstrapLabels = new[] { AddressablesLableNames.Sprite, AddressablesLableNames.Prefab, AddressablesLableNames.UI };

            await PreloadMultipleLabelsAsync<UnityEngine.Object>(
                bootstrapLabels,
                onProgress,
                () => Debug.Log("[预加载服务] 启动资源预加载完成")
            );
        }

        /// <summary>
        /// 批量预加载多个标签
        /// </summary>
        public async UniTask PreloadMultipleLabelsAsync<T>(
            string[] labels,
            Action<float> onProgress = null,
            Action onComplete = null) where T : UnityEngine.Object
        {
            await _labelStrategy.PreloadMultipleLabelsAsync<T>(labels, onProgress, onComplete);
        }
    }
}