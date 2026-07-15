using UnityEngine;
using UnityEngine.UI;

namespace Dragon.GameUI
{
    /// <summary>
    /// PopupRoot, 弹窗根节点
    /// </summary>
    public class PopupRoot : MonoBehaviour
    {
        private static PopupRoot _instance;
        private Canvas _canvas;

        internal static PopupRoot Create()
        {
            if (_instance == null)
            {
                var go = new GameObject("[PopupRoot]", typeof(RectTransform));
                _instance = go.AddComponent<PopupRoot>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }

        public void Init()
        {
            var rect = _instance.gameObject.GetComponent<RectTransform>();
            rect.SetParent(GameUIManager.Instance.GetUIRootTransform(), false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;

            _canvas = _instance.gameObject.AddComponent<Canvas>();
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = UIDefine.UISortingType.Popup;            
            _instance.gameObject.AddComponent<GraphicRaycaster>();
        }

        public Transform GetPopupCanvasTransform()
        {
            return _instance.transform;
        }

        private void OnDestroy()
        {
            _instance = null;
        }
    }
}
