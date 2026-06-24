using UnityEngine;
using UnityEngine.UI;

namespace Dragon.GameUI
{
    /// <summary>
    /// Toast 根节点，挂载在 UIRoot 下，使用 Notification 排序层级。
    /// </summary>
    public class ToastRoot : MonoBehaviour
    {
        private static ToastRoot _instance;
        private Canvas _canvas;

        internal static ToastRoot Create()
        {
            if (_instance != null)
            {
                return _instance;
            }

            var go = new GameObject("[ToastRoot]", typeof(RectTransform));
            _instance = go.AddComponent<ToastRoot>();
            DontDestroyOnLoad(go);
            return _instance;
        }

        public void Init()
        {
            Transform uiRootTransform = GameUIManager.Instance.GetUIRootTransform();
            var rect = _instance.GetComponent<RectTransform>();
            rect.SetParent(uiRootTransform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;

            _canvas = _instance.gameObject.AddComponent<Canvas>();
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = UIDefine.UISortingType.Notification;
        }

        public Transform GetToastRootTransform()
        {
            return _instance.transform;
        }

        private void OnDestroy()
        {
            _instance = null;
        }
    }
}
