using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Dragon.GameUI
{
    /// <summary>
    /// 单条弹幕滚动宿主：从右入屏滚至左出屏后销毁，并向 Manager 回报「完整入屏」。
    /// </summary>
    public class UIDanmakuItem : MonoBehaviour
    {
        private RectTransform _rect;
        private RectTransform _rootRect;
        private CanvasGroup _canvasGroup;
        private Tween _moveTween;
        private float _gap;
        private bool _hasReportedFullyEntered;
        private bool _destroyOnComplete = true;
        private Action<UIDanmakuItem> _onFullyEntered;
        private Action<UIDanmakuItem> _onCompleted;

        public float Gap => _gap;
        public bool HasReportedFullyEntered => _hasReportedFullyEntered;

        private void Awake()
        {
            _rect = transform as RectTransform;
            EnsureCanvasGroup();
        }

        /// <summary>
        /// 配置并开始从右向左滚动。
        /// </summary>
        /// <param name="rootRect">弹幕根 Rect（全屏）。</param>
        /// <param name="topOffset">距屏幕顶像素。</param>
        /// <param name="gap">与下一条的最小间距（像素），用于完整入屏判定。</param>
        /// <param name="pixelsPerSecond">水平速度。</param>
        /// <param name="destroyOnComplete">滚出后是否销毁。</param>
        /// <param name="onFullyEntered">右边缘离开屏幕右缘至少 gap 时回调一次。</param>
        /// <param name="onCompleted">滚出左侧结束回调。</param>
        public void Play(
            RectTransform rootRect,
            float topOffset,
            float gap,
            float pixelsPerSecond,
            bool destroyOnComplete,
            Action<UIDanmakuItem> onFullyEntered,
            Action<UIDanmakuItem> onCompleted)
        {
            _rootRect = rootRect;
            _gap = Mathf.Max(0f, gap);
            _destroyOnComplete = destroyOnComplete;
            _onFullyEntered = onFullyEntered;
            _onCompleted = onCompleted;
            _hasReportedFullyEntered = false;

            if (_rect == null)
            {
                _rect = transform as RectTransform;
            }

            EnsureCanvasGroup();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);

            float rootWidth = _rootRect.rect.width;
            float itemWidth = Mathf.Max(_rect.rect.width, 1f);

            // 顶对齐：锚点顶中，pivot 左上，Y = -topOffset
            _rect.anchorMin = new Vector2(0.5f, 1f);
            _rect.anchorMax = new Vector2(0.5f, 1f);
            _rect.pivot = new Vector2(0f, 1f);
            _rect.localScale = Vector3.one;

            float startX = rootWidth * 0.5f;
            float endX = -rootWidth * 0.5f - itemWidth;
            float travel = startX - endX;
            float speed = Mathf.Max(1f, pixelsPerSecond);
            float duration = travel / speed;

            _rect.anchoredPosition = new Vector2(startX, -Mathf.Max(0f, topOffset));

            KillMoveTween();
            _moveTween = _rect
                .DOAnchorPosX(endX, duration)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnUpdate(CheckFullyEntered)
                .OnComplete(HandleCompleted);
        }

        /// <summary>当前条目右边缘（相对根节点本地 X）。</summary>
        public float GetRightEdgeLocalX()
        {
            if (_rect == null)
            {
                return 0f;
            }

            // pivot.x = 0 → 左缘在 anchoredPosition.x，右缘 = x + width
            return _rect.anchoredPosition.x + _rect.rect.width;
        }

        public bool IsFullyEntered(float canvasRight, float gap)
        {
            return GetRightEdgeLocalX() <= canvasRight - gap;
        }

        public bool IsFullyExited(float canvasLeft)
        {
            return GetRightEdgeLocalX() < canvasLeft;
        }

        private void CheckFullyEntered()
        {
            if (_hasReportedFullyEntered || _rootRect == null)
            {
                return;
            }

            float canvasRight = _rootRect.rect.width * 0.5f;
            if (!IsFullyEntered(canvasRight, _gap))
            {
                return;
            }

            _hasReportedFullyEntered = true;
            _onFullyEntered?.Invoke(this);
        }

        private void HandleCompleted()
        {
            _moveTween = null;
            _onCompleted?.Invoke(this);

            if (_destroyOnComplete)
            {
                Destroy(gameObject);
            }
        }

        private void EnsureCanvasGroup()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        private void KillMoveTween()
        {
            if (_moveTween == null || !_moveTween.IsActive())
            {
                _moveTween = null;
                return;
            }

            _moveTween.Kill();
            _moveTween = null;
        }

        private void OnDestroy()
        {
            KillMoveTween();
            _onFullyEntered = null;
            _onCompleted = null;
        }
    }
}
