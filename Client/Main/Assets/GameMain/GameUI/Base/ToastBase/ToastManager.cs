using Dragon.Resource;
using UnityEngine;

namespace Dragon.GameUI
{
    /// <summary>
    /// 浮动 Toast 管理器：每次 Show 单独实例化预制体，支持同名多条并存。
    /// </summary>
    public class ToastManager
    {
        public const string DefaultToastAddress = "ui_toast";

        #region Singleton

        private static ToastManager _instance;
        private static ToastRoot _toastRoot;

        public static ToastManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ToastManager();
                    _toastRoot = ToastRoot.Create();
                    _toastRoot.Init();
                }

                return _instance;
            }
        }

        #endregion

        /// <summary>
        /// 显示一条 Toast。
        /// </summary>
        /// <param name="message">显示文本。</param>
        /// <param name="toastAddress">Addressables 地址，默认 <see cref="DefaultToastAddress"/>。</param>
        /// <param name="displayDuration">显示时长（秒），&lt; 0 使用预制体默认值。</param>
        /// <param name="fadeDuration">渐隐时长（秒），&lt; 0 使用预制体默认值。</param>
        public void Show(
            string message,
            string toastAddress = DefaultToastAddress,
            float displayDuration = -1f,
            float fadeDuration = -1f)
        {
            if (string.IsNullOrEmpty(message))
            {
                Debug.LogWarning("[ToastManager] Show ignored, message is empty.");
                return;
            }

            if (string.IsNullOrEmpty(toastAddress))
            {
                Debug.LogError("[ToastManager] Show failed, toastAddress is empty.");
                return;
            }

            ResourceManager.Instance.LoadAsync<GameObject>(toastAddress, prefab =>
            {
                if (prefab == null)
                {
                    Debug.LogError($"[ToastManager] Show failed, prefab not found: {toastAddress}");
                    return;
                }

                if (_toastRoot == null)
                {
                    Debug.LogError("[ToastManager] Show failed, ToastRoot is not ready.");
                    return;
                }

                GameObject toastObject = Object.Instantiate(prefab, _toastRoot.GetToastRootTransform());
                toastObject.name = prefab.name;

                UIToast toast = toastObject.GetComponent<UIToast>();
                if (toast == null)
                {
                    Debug.LogError($"[ToastManager] Show failed, UIToast not found on prefab: {toastAddress}");
                    Object.Destroy(toastObject);
                    return;
                }

                toast.Show(message, displayDuration, fadeDuration);
            }, ex =>
            {
                Debug.LogError($"[ToastManager] Show failed to load prefab: {toastAddress}, {ex.Message}");
            });
        }
    }
}
