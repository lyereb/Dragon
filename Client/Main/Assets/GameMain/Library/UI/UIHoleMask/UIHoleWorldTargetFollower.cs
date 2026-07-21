using UnityEngine;

namespace Dragon.GameUI
{
    /// <summary>
    /// 将全屏 Canvas 下的代理 RectTransform 对齐到场景 Renderer/Transform 的屏幕包围盒，
    /// 供 <see cref="UIHoleMaskGraphic"/> 镂空使用。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("UI/UIHole World Target Follower", 201)]
    public class UIHoleWorldTargetFollower : MonoBehaviour
    {
        [SerializeField] private Camera _worldCamera;
        [SerializeField] private Transform _worldTarget;
        [SerializeField] private Renderer _worldRenderer;
        [SerializeField] private Vector2 _screenPadding;
        [SerializeField] private bool _hideWhenBehindCamera = true;

        private RectTransform _proxyRect;
        private RectTransform _parentRect;
        private Canvas _rootCanvas;

        public RectTransform ProxyRect => _proxyRect;

        private void Awake()
        {
            _proxyRect = transform as RectTransform;
            _parentRect = _proxyRect.parent as RectTransform;
            _rootCanvas = _proxyRect.GetComponentInParent<Canvas>();
        }

        private void LateUpdate()
        {
            RefreshProxyRect();
        }

        /// <summary>跟随场景 Transform（优先使用 Renderer.bounds）。</summary>
        public void SetTarget(Transform worldTarget)
        {
            _worldTarget = worldTarget;
            _worldRenderer = worldTarget != null ? worldTarget.GetComponentInChildren<Renderer>(true) : null;
            enabled = worldTarget != null;
            RefreshProxyRect();
        }

        /// <summary>跟随场景 Renderer。</summary>
        public void SetTarget(Renderer worldRenderer)
        {
            _worldRenderer = worldRenderer;
            _worldTarget = worldRenderer != null ? worldRenderer.transform : null;
            enabled = worldRenderer != null;
            RefreshProxyRect();
        }

        public void ClearTarget()
        {
            _worldTarget = null;
            _worldRenderer = null;
            enabled = false;
            if (_proxyRect != null)
            {
                _proxyRect.sizeDelta = Vector2.zero;
            }
        }

        private void RefreshProxyRect()
        {
            if (_proxyRect == null || _parentRect == null)
            {
                return;
            }

            if (_worldTarget == null)
            {
                _proxyRect.sizeDelta = Vector2.zero;
                return;
            }

            Camera worldCamera = ResolveWorldCamera();
            if (worldCamera == null)
            {
                return;
            }

            Bounds bounds = _worldRenderer != null
                ? _worldRenderer.bounds
                : new Bounds(_worldTarget.position, Vector3.zero);

            if (!TryProjectBoundsToScreen(bounds, worldCamera, out float screenMinX, out float screenMinY,
                    out float screenMaxX, out float screenMaxY))
            {
                _proxyRect.sizeDelta = Vector2.zero;
                return;
            }

            screenMinX -= _screenPadding.x;
            screenMinY -= _screenPadding.y;
            screenMaxX += _screenPadding.x;
            screenMaxY += _screenPadding.y;

            Camera uiCamera = ResolveUiCamera();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _parentRect,
                    new Vector2(screenMinX, screenMinY),
                    uiCamera,
                    out Vector2 localMin))
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _parentRect,
                    new Vector2(screenMaxX, screenMaxY),
                    uiCamera,
                    out Vector2 localMax))
            {
                return;
            }

            if (localMin.x > localMax.x)
            {
                (localMin.x, localMax.x) = (localMax.x, localMin.x);
            }

            if (localMin.y > localMax.y)
            {
                (localMin.y, localMax.y) = (localMax.y, localMin.y);
            }

            Vector2 center = (localMin + localMax) * 0.5f;
            Vector2 size = localMax - localMin;
            if (size.x <= 0f || size.y <= 0f)
            {
                _proxyRect.sizeDelta = Vector2.zero;
                return;
            }

            _proxyRect.anchorMin = _proxyRect.anchorMax = new Vector2(0.5f, 0.5f);
            _proxyRect.pivot = new Vector2(0.5f, 0.5f);
            _proxyRect.anchoredPosition = center;
            _proxyRect.sizeDelta = size;
        }

        private Camera ResolveWorldCamera()
        {
            if (_worldCamera != null)
            {
                return _worldCamera;
            }

            return Camera.main;
        }

        private Camera ResolveUiCamera()
        {
            if (_rootCanvas == null)
            {
                _rootCanvas = _proxyRect.GetComponentInParent<Canvas>();
            }

            if (_rootCanvas != null && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                return _rootCanvas.worldCamera;
            }

            return null;
        }

        private bool TryProjectBoundsToScreen(
            Bounds bounds,
            Camera worldCamera,
            out float screenMinX,
            out float screenMinY,
            out float screenMaxX,
            out float screenMaxY)
        {
            screenMinX = float.MaxValue;
            screenMinY = float.MaxValue;
            screenMaxX = float.MinValue;
            screenMaxY = float.MinValue;

            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            bool hasVisiblePoint = false;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = center + new Vector3(
                    (i & 1) == 0 ? -extents.x : extents.x,
                    (i & 2) == 0 ? -extents.y : extents.y,
                    (i & 4) == 0 ? -extents.z : extents.z);

                Vector3 screenPoint = worldCamera.WorldToScreenPoint(corner);
                if (_hideWhenBehindCamera && screenPoint.z < 0f)
                {
                    continue;
                }

                hasVisiblePoint = true;
                screenMinX = Mathf.Min(screenMinX, screenPoint.x);
                screenMinY = Mathf.Min(screenMinY, screenPoint.y);
                screenMaxX = Mathf.Max(screenMaxX, screenPoint.x);
                screenMaxY = Mathf.Max(screenMaxY, screenPoint.y);
            }

            return hasVisiblePoint;
        }
    }
}
