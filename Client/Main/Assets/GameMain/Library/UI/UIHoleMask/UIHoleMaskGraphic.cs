using UnityEngine;
using UnityEngine.UI;

namespace Dragon.GameUI
{
    /// <summary>
    /// 全屏/区域半透明遮罩，按目标 RectTransform 挖出圆角矩形镂空（无自定义 Shader，改 Mesh）。
    /// 挂在全屏 Image 同级节点上使用，勿与 Image 同挂（本组件本身就是 Graphic）。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("UI/UIHole Mask Graphic", 200)]
    public class UIHoleMaskGraphic : MaskableGraphic, ICanvasRaycastFilter
    {
        #region Hole

        [SerializeField] private RectTransform _holeTarget;
        [SerializeField] private float _radius = 16f;
        [SerializeField] private float _padding;
        [SerializeField] [Range(2, 24)] private int _cornerSegments = 8;
        [SerializeField] private bool _raycastThroughHole = true;

        #endregion

        private readonly Vector3[] _worldCorners = new Vector3[4];

        /// <summary>镂空目标；为 null 时整屏遮罩。</summary>
        public RectTransform HoleTarget
        {
            get => _holeTarget;
            set
            {
                if (_holeTarget == value)
                {
                    return;
                }

                _holeTarget = value;
                SetVerticesDirty();
            }
        }

        public float Radius
        {
            get => _radius;
            set
            {
                _radius = Mathf.Max(0f, value);
                SetVerticesDirty();
            }
        }

        public float Padding
        {
            get => _padding;
            set
            {
                _padding = value;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// 设置镂空目标与圆角；radius &lt; 0 时保持当前圆角。
        /// </summary>
        public void SetHoleTarget(RectTransform target, float radius = -1f, float padding = float.NaN)
        {
            _holeTarget = target;
            if (radius >= 0f)
            {
                _radius = radius;
            }

            if (!float.IsNaN(padding))
            {
                _padding = padding;
            }

            SetVerticesDirty();
        }

        /// <summary>清除镂空，恢复整屏遮罩。</summary>
        public void ClearHole()
        {
            _holeTarget = null;
            SetVerticesDirty();
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!_raycastThroughHole || _holeTarget == null)
            {
                return true;
            }

            if (!TryGetHoleLocalRect(out Vector2 holeMin, out Vector2 holeMax, out float radius))
            {
                return true;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, screenPoint, eventCamera, out Vector2 localPoint))
            {
                return true;
            }

            // 点在圆角矩形洞内 → 不拦截射线
            return !IsPointInsideRoundedRect(localPoint, holeMin, holeMax, radius);
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            color = new Color(0f, 0f, 0f, 0.7f);
            raycastTarget = true;
        }
#endif

        protected override void OnEnable()
        {
            base.OnEnable();
            SetVerticesDirty();
        }

        private void LateUpdate()
        {
            if (_holeTarget != null)
            {
                SetVerticesDirty();
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _radius = Mathf.Max(0f, _radius);
            _cornerSegments = Mathf.Clamp(_cornerSegments, 2, 24);
            SetVerticesDirty();
        }
#endif

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect outer = rectTransform.rect;
            if (outer.width <= 0f || outer.height <= 0f)
            {
                return;
            }

            if (!TryGetHoleLocalRect(out Vector2 holeMin, out Vector2 holeMax, out float radius))
            {
                AddQuad(vh, outer.min, outer.max, color);
                return;
            }

            BuildFrameWithRoundedHole(vh, outer, holeMin, holeMax, radius);
        }

        /// <summary>
        /// 将目标世界四角转到本节点本地坐标，并做 padding / 夹紧 / 圆角上限。
        /// </summary>
        private bool TryGetHoleLocalRect(out Vector2 holeMin, out Vector2 holeMax, out float radius)
        {
            holeMin = default;
            holeMax = default;
            radius = 0f;

            if (_holeTarget == null)
            {
                return false;
            }

            _holeTarget.GetWorldCorners(_worldCorners);
            Vector2 min = rectTransform.InverseTransformPoint(_worldCorners[0]);
            Vector2 max = rectTransform.InverseTransformPoint(_worldCorners[2]);

            // 容错：偶发轴翻转
            if (min.x > max.x)
            {
                (min.x, max.x) = (max.x, min.x);
            }

            if (min.y > max.y)
            {
                (min.y, max.y) = (max.y, min.y);
            }

            min -= Vector2.one * _padding;
            max += Vector2.one * _padding;

            Rect outer = rectTransform.rect;
            min = Vector2.Max(min, outer.min);
            max = Vector2.Min(max, outer.max);

            if (max.x <= min.x || max.y <= min.y)
            {
                return false;
            }

            holeMin = min;
            holeMax = max;
            radius = Mathf.Min(_radius, (max.x - min.x) * 0.5f, (max.y - min.y) * 0.5f);
            return true;
        }

        private void BuildFrameWithRoundedHole(
            VertexHelper vh, Rect outer, Vector2 holeMin, Vector2 holeMax, float radius)
        {
            float left = holeMin.x;
            float right = holeMax.x;
            float bottom = holeMin.y;
            float top = holeMax.y;

            // 四边矩形条（圆角为 0 时即可拼出完整直角洞）
            AddQuad(vh, new Vector2(outer.xMin, top), new Vector2(outer.xMax, outer.yMax), color);
            AddQuad(vh, new Vector2(outer.xMin, outer.yMin), new Vector2(outer.xMax, bottom), color);
            AddQuad(vh, new Vector2(outer.xMin, bottom), new Vector2(left, top), color);
            AddQuad(vh, new Vector2(right, bottom), new Vector2(outer.xMax, top), color);

            if (radius <= 0.01f)
            {
                return;
            }

            // 四角：外直角点 → 内圆弧，填满圆角外侧遮罩区
            AddCornerFan(vh, new Vector2(left, top), new Vector2(left + radius, top - radius), radius, 90f, 180f);
            AddCornerFan(vh, new Vector2(right, top), new Vector2(right - radius, top - radius), radius, 0f, 90f);
            AddCornerFan(vh, new Vector2(right, bottom), new Vector2(right - radius, bottom + radius), radius, 270f, 360f);
            AddCornerFan(vh, new Vector2(left, bottom), new Vector2(left + radius, bottom + radius), radius, 180f, 270f);
        }

        /// <summary>
        /// 从圆角矩形外角点连到四分之一圆弧，覆盖洞角外侧区域。
        /// </summary>
        private void AddCornerFan(
            VertexHelper vh, Vector2 outerCorner, Vector2 center, float radius, float degFrom, float degTo)
        {
            int start = vh.currentVertCount;
            vh.AddVert(outerCorner, color, Vector2.zero);

            int segments = _cornerSegments;
            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Lerp(degFrom, degTo, i / (float)segments) * Mathf.Deg2Rad;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vh.AddVert(point, color, Vector2.zero);
            }

            for (int i = 0; i < segments; i++)
            {
                vh.AddTriangle(start, start + 1 + i, start + 2 + i);
            }
        }

        private static void AddQuad(VertexHelper vh, Vector2 min, Vector2 max, Color32 col)
        {
            if (max.x <= min.x || max.y <= min.y)
            {
                return;
            }

            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(min.x, min.y), col, Vector2.zero);
            vh.AddVert(new Vector3(min.x, max.y), col, Vector2.zero);
            vh.AddVert(new Vector3(max.x, max.y), col, Vector2.zero);
            vh.AddVert(new Vector3(max.x, min.y), col, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }

        private static bool IsPointInsideRoundedRect(Vector2 point, Vector2 min, Vector2 max, float radius)
        {
            if (point.x < min.x || point.x > max.x || point.y < min.y || point.y > max.y)
            {
                return false;
            }

            if (radius <= 0.01f)
            {
                return true;
            }

            // 落在四角圆角区时，需在对应四分之一圆内
            float left = min.x + radius;
            float right = max.x - radius;
            float bottom = min.y + radius;
            float top = max.y - radius;

            if (point.x >= left && point.x <= right)
            {
                return true;
            }

            if (point.y >= bottom && point.y <= top)
            {
                return true;
            }

            Vector2 cornerCenter;
            if (point.x < left && point.y > top)
            {
                cornerCenter = new Vector2(left, top);
            }
            else if (point.x > right && point.y > top)
            {
                cornerCenter = new Vector2(right, top);
            }
            else if (point.x > right && point.y < bottom)
            {
                cornerCenter = new Vector2(right, bottom);
            }
            else
            {
                cornerCenter = new Vector2(left, bottom);
            }

            return (point - cornerCenter).sqrMagnitude <= radius * radius;
        }
    }
}
