using UnityEngine;

namespace Dragon.GameUI
{
    /// <summary>
    /// 按名称或层级路径查找下层 UI，并将其包围盒同步到代理 RectTransform，
    /// 供 <see cref="UIHoleMaskGraphic"/> 镂空使用。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("UI/UIHole UI Target Follower", 202)]
    public class UIHoleUiTargetFollower : MonoBehaviour
    {
        [SerializeField] private RectTransform _uiTarget;
        [SerializeField] private Transform _searchRoot;
        [SerializeField] private Vector2 _padding;

        private RectTransform _proxyRect;
        private RectTransform _parentRect;
        private readonly Vector3[] _uiCorners = new Vector3[4];

        public RectTransform ProxyRect => _proxyRect;

        private void Awake()
        {
            _proxyRect = transform as RectTransform;
            _parentRect = _proxyRect.parent as RectTransform;
        }

        private void LateUpdate()
        {
            RefreshProxyRect();
        }

        /// <summary>直接跟随指定 UI RectTransform。</summary>
        public void SetTarget(RectTransform uiTarget)
        {
            _uiTarget = uiTarget;
            enabled = uiTarget != null;
            RefreshProxyRect();
        }

        /// <summary>
        /// 按对象名查找 UI 并跟随；name 含 '/' 时按层级路径解析（如 ButtonList1/BtnStart）。
        /// </summary>
        public void SetTargetByName(string nameOrPath, Transform searchRoot = null)
        {
            RectTransform found = UIHoleUiTargetFinder.Find(nameOrPath, searchRoot ?? _searchRoot);
            SetTarget(found);
        }

        public void ClearTarget()
        {
            _uiTarget = null;
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

            if (_uiTarget == null)
            {
                _proxyRect.sizeDelta = Vector2.zero;
                return;
            }

            if (!_uiTarget.gameObject.activeInHierarchy)
            {
                _proxyRect.sizeDelta = Vector2.zero;
                return;
            }

            _uiTarget.GetWorldCorners(_uiCorners);
            Vector2 localMin = _parentRect.InverseTransformPoint(_uiCorners[0]);
            Vector2 localMax = localMin;
            for (int i = 1; i < _uiCorners.Length; i++)
            {
                Vector2 point = _parentRect.InverseTransformPoint(_uiCorners[i]);
                localMin = Vector2.Min(localMin, point);
                localMax = Vector2.Max(localMax, point);
            }

            localMin -= _padding;
            localMax += _padding;
            ApplyLocalRectToProxy(localMin, localMax);
        }

        private void ApplyLocalRectToProxy(Vector2 localMin, Vector2 localMax)
        {
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
    }

    /// <summary>在 UIRoot 或指定根节点下查找 UI RectTransform。</summary>
    public static class UIHoleUiTargetFinder
    {
        public static RectTransform Find(string nameOrPath, Transform searchRoot = null)
        {
            if (string.IsNullOrEmpty(nameOrPath))
            {
                return null;
            }

            Transform found = nameOrPath.IndexOf('/') >= 0
                ? FindByPath(nameOrPath, searchRoot)
                : FindByName(nameOrPath, searchRoot);

            if (found == null)
            {
                return null;
            }

            return found as RectTransform ?? found.GetComponent<RectTransform>();
        }

        private static Transform FindByName(string name, Transform searchRoot)
        {
            if (searchRoot != null)
            {
                Transform found = FindTransformByName(searchRoot, name);
                if (found != null)
                {
                    return found;
                }
            }

            GameObject activeObject = GameObject.Find(name);
            if (activeObject != null)
            {
                return activeObject.transform;
            }

            Transform uiRoot = ResolveDefaultSearchRoot();
            if (uiRoot != null)
            {
                return FindTransformByName(uiRoot, name);
            }

            return null;
        }

        private static Transform FindByPath(string path, Transform searchRoot)
        {
            string[] parts = path.Split('/');
            if (parts.Length == 0)
            {
                return null;
            }

            Transform current = searchRoot;
            int startIndex = 0;

            if (current == null)
            {
                current = FindByName(parts[0], null);
                startIndex = 1;
            }
            else if (current.name != parts[0])
            {
                current = FindTransformByName(current, parts[0]);
                startIndex = 1;
            }

            for (int i = startIndex; i < parts.Length; i++)
            {
                if (current == null)
                {
                    return null;
                }

                current = FindDirectOrDeepChild(current, parts[i]);
            }

            return current;
        }

        private static Transform FindDirectOrDeepChild(Transform parent, string childName)
        {
            Transform direct = parent.Find(childName);
            if (direct != null)
            {
                return direct;
            }

            return FindTransformByName(parent, childName);
        }

        private static Transform FindTransformByName(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindTransformByName(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform ResolveDefaultSearchRoot()
        {
            GameObject uiRoot = GameObject.Find("[UIRoot]");
            return uiRoot != null ? uiRoot.transform : null;
        }
    }
}
