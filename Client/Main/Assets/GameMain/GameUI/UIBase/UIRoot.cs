using UnityEngine;
using UnityEngine.UI;

namespace Dragon.GameUI
{
    /// <summary>
    /// UIRoot, UI根节点
    /// </summary>
    public class UIRoot : MonoBehaviour
    {
        private static UIRoot _instance;
        private CanvasScaler _canvasScaler;
        internal static UIRoot Create()
        {
            if (_instance == null)
            {
                var go = new GameObject("[UIRoot]");
                _instance = go.AddComponent<UIRoot>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }

        public void Init()
        {
            //初始化canvas
            var canvas = _instance.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            canvas.overrideSorting = true;
            _canvasScaler = _instance.gameObject.AddComponent<CanvasScaler>();
            _instance.gameObject.AddComponent<GraphicRaycaster>();

            //设置UI缩放模式
            _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _canvasScaler.referenceResolution = new Vector2(1920, 1080);
            _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _canvasScaler.matchWidthOrHeight = 0;

            CreateMainUIRootTransform();
            CreateSingleUIRootTransform();
        }

        public void CreateMainUIRootTransform()
        {
            var go = new GameObject("[MainUIRoot]", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_instance.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
        }

        public void CreateSingleUIRootTransform()
        {
            var go = new GameObject("[SingleUIRoot]", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_instance.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
        }

        #region 对外接口
        public Transform GetUIRootTransform()
        {
            return _instance.transform;
        }

        public Transform GetMainUIRootTransform()
        {
            return _instance.transform.Find("[MainUIRoot]");
        }

        public Transform GetSingleUIRootTransform()
        {
            return _instance.transform.Find("[SingleUIRoot]");
        }
        #endregion
    }
}
