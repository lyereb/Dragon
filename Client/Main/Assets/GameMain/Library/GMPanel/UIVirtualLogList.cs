using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dragon.Library
{
    [DisallowMultipleComponent]
    public class UIVirtualLogList : MonoBehaviour
    {
        private const float BottomThreshold = 0.02f;

        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;
        [SerializeField] private UILogLineItem _lineItemPrefab;
        [SerializeField] private int _poolBuffer = 4;
        [SerializeField] private float _minLineHeight = 44f;
        [SerializeField] private float _lineVerticalPadding = 8f;

        private readonly List<UILogLineItem> _pool = new List<UILogLineItem>();
        private readonly List<UILogLineItem> _activeItems = new List<UILogLineItem>();
        private readonly List<int> _filteredSourceIndices = new List<int>();
        private readonly List<float> _lineHeights = new List<float>();
        private readonly List<float> _prefixHeights = new List<float>();

        private DebugLogManager _manager;
        private DebugLogFilter _filter = DebugLogFilter.All;
        private UILogLineItem _measureItem;
        private RectTransform _viewport;
        private float _contentWidth;
        private bool _followBottom = true;
        private bool _initialized;
        private Coroutine _scrollCoroutine;

        public bool FollowBottom => _followBottom;

        private void Awake()
        {
            if (_scrollRect == null)
            {
                _scrollRect = GetComponent<ScrollRect>();
            }

            if (_scrollRect != null)
            {
                _viewport = _scrollRect.viewport;
                if (_content == null)
                {
                    _content = _scrollRect.content;
                }

                _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
            }
        }

        private void OnDestroy()
        {
            if (_scrollRect != null)
            {
                _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!_initialized || _content == null)
            {
                return;
            }

            float width = GetContentWidth();
            if (Mathf.Approximately(width, _contentWidth))
            {
                return;
            }

            _contentWidth = width;
            RebuildHeights();
            RefreshVisibleItems();
            UpdateContentSize();
            if (_followBottom)
            {
                ScrollToBottomImmediate();
            }
        }

        public void Initialize(DebugLogManager manager)
        {
            _manager = manager;
            _initialized = manager != null && _scrollRect != null && _content != null && _lineItemPrefab != null;
            if (!_initialized)
            {
                return;
            }

            EnsureMeasureItem();
            _contentWidth = GetContentWidth();
            ConfigureContentTransform();
        }

        public void SetFollowBottom(bool followBottom)
        {
            _followBottom = followBottom;
        }

        public void SetFilter(DebugLogFilter filter, bool scrollToBottom)
        {
            _filter = filter;
            if (!_initialized)
            {
                return;
            }

            if (GetContentWidth() <= 0.01f)
            {
                StartCoroutine(DelayedSetFilter(scrollToBottom));
                return;
            }

            ApplyFilter(scrollToBottom);
        }

        private void ApplyFilter(bool scrollToBottom)
        {
            RebuildFilteredIndices();
            RebuildHeights();
            RefreshVisibleItems();
            UpdateContentSize();

            if (scrollToBottom)
            {
                RequestScrollToBottom();
            }
            else
            {
                ClampScrollPosition();
            }
        }

        private IEnumerator DelayedSetFilter(bool scrollToBottom)
        {
            yield return null;
            _contentWidth = GetContentWidth();
            ApplyFilter(scrollToBottom);
        }

        public void HandleChange(DebugLogChangeKind changeKind, bool followBottom)
        {
            if (!_initialized)
            {
                return;
            }

            _followBottom = followBottom;
            float previousScrollOffset = GetScrollOffset();
            float previousTotalHeight = GetTotalHeight();
            float viewportHeight = GetViewportHeight();
            bool wasAtBottom = IsNearBottom();

            switch (changeKind)
            {
                case DebugLogChangeKind.Clear:
                    RebuildFilteredIndices();
                    RebuildHeights();
                    RecycleAllActiveItems();
                    UpdateContentSize();
                    ResetScrollPosition();
                    break;

                case DebugLogChangeKind.Append:
                    AppendFilteredData();
                    RefreshVisibleItems();
                    UpdateContentSize();
                    if (_followBottom || wasAtBottom)
                    {
                        RequestScrollToBottom();
                    }

                    break;

                case DebugLogChangeKind.TrimHead:
                    RebuildFilteredIndices();
                    RebuildHeights();
                    RefreshVisibleItems();
                    UpdateContentSize();
                    if (_followBottom || wasAtBottom)
                    {
                        RequestScrollToBottom();
                    }
                    else
                    {
                        RestoreScrollAfterResize(previousScrollOffset, previousTotalHeight, viewportHeight);
                    }

                    break;
            }
        }

        public void RequestScrollToBottom()
        {
            if (_scrollCoroutine != null)
            {
                StopCoroutine(_scrollCoroutine);
            }

            _scrollCoroutine = StartCoroutine(ScrollToBottomWhenReady());
        }

        private void OnScrollValueChanged(Vector2 _)
        {
            if (_scrollRect == null)
            {
                return;
            }

            _followBottom = _scrollRect.verticalNormalizedPosition <= BottomThreshold;
            RefreshVisibleItems();
        }

        private void ConfigureContentTransform()
        {
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;
        }

        private void EnsureMeasureItem()
        {
            if (_measureItem != null)
            {
                return;
            }

            _measureItem = Instantiate(_lineItemPrefab, _content);
            _measureItem.name = "MeasureItem";
            _measureItem.gameObject.SetActive(false);
        }

        private void RebuildFilteredIndices()
        {
            _filteredSourceIndices.Clear();
            for (int i = 0; i < _manager.Count; i++)
            {
                DebugLogEntry entry = _manager.GetEntry(i);
                if (DebugLogManager.MatchesFilter(entry.Type, _filter))
                {
                    _filteredSourceIndices.Add(i);
                }
            }
        }

        private void AppendFilteredData()
        {
            if (_manager.Count == 0)
            {
                _filteredSourceIndices.Clear();
                _lineHeights.Clear();
                _prefixHeights.Clear();
                return;
            }

            int sourceIndex = _manager.Count - 1;
            DebugLogEntry entry = _manager.GetEntry(sourceIndex);
            if (!DebugLogManager.MatchesFilter(entry.Type, _filter))
            {
                return;
            }

            _filteredSourceIndices.Add(sourceIndex);
            AppendHeightForEntry(entry);
        }

        private void RebuildHeights()
        {
            _lineHeights.Clear();
            _prefixHeights.Clear();
            _prefixHeights.Add(0f);

            float width = GetContentWidth();
            for (int i = 0; i < _filteredSourceIndices.Count; i++)
            {
                DebugLogEntry entry = _manager.GetEntry(_filteredSourceIndices[i]);
                float height = MeasureEntryHeight(entry, width);
                _lineHeights.Add(height);
                _prefixHeights.Add(_prefixHeights[_prefixHeights.Count - 1] + height);
            }
        }

        private void AppendHeightForEntry(in DebugLogEntry entry)
        {
            float width = GetContentWidth();
            float height = MeasureEntryHeight(entry, width);
            _lineHeights.Add(height);

            float total = _prefixHeights.Count > 0 ? _prefixHeights[_prefixHeights.Count - 1] : 0f;
            _prefixHeights.Add(total + height);
        }

        private float MeasureEntryHeight(in DebugLogEntry entry, float width)
        {
            _measureItem.SetText(DebugLogManager.FormatDisplayText(entry));
            return _measureItem.MeasureHeight(width, _minLineHeight, _lineVerticalPadding);
        }

        private void UpdateContentSize()
        {
            float totalHeight = GetTotalHeight();
            _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
        }

        private void RefreshVisibleItems()
        {
            if (_filteredSourceIndices.Count == 0)
            {
                RecycleAllActiveItems();
                return;
            }

            float scrollOffset = GetScrollOffset();
            float viewportHeight = GetViewportHeight();
            FindVisibleRange(scrollOffset, viewportHeight, out int firstIndex, out int lastIndex);

            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                UILogLineItem item = _activeItems[i];
                if (item.BoundIndex < firstIndex || item.BoundIndex > lastIndex)
                {
                    RecycleItem(item);
                }
            }

            int requiredCount = lastIndex - firstIndex + 1;
            EnsurePoolSize(requiredCount);

            for (int index = firstIndex; index <= lastIndex; index++)
            {
                UILogLineItem item = FindActiveItem(index);
                if (item == null)
                {
                    item = GetItemFromPool();
                    _activeItems.Add(item);
                }

                BindItem(item, index);
            }
        }

        private void BindItem(UILogLineItem item, int filteredIndex)
        {
            DebugLogEntry entry = _manager.GetEntry(_filteredSourceIndices[filteredIndex]);
            item.BoundIndex = filteredIndex;
            item.SetText(DebugLogManager.FormatDisplayText(entry));
            item.gameObject.SetActive(true);
            item.transform.SetAsLastSibling();
            item.Layout(GetContentWidth(), _lineHeights[filteredIndex], _prefixHeights[filteredIndex]);
        }

        private UILogLineItem FindActiveItem(int filteredIndex)
        {
            for (int i = 0; i < _activeItems.Count; i++)
            {
                if (_activeItems[i].BoundIndex == filteredIndex)
                {
                    return _activeItems[i];
                }
            }

            return null;
        }

        private void FindVisibleRange(float scrollOffset, float viewportHeight, out int firstIndex, out int lastIndex)
        {
            float top = scrollOffset;
            float bottom = scrollOffset + viewportHeight;
            firstIndex = FindFirstIndexAtOffset(top);
            lastIndex = FindLastIndexBeforeOffset(bottom);
            firstIndex = Mathf.Clamp(firstIndex, 0, Mathf.Max(0, _filteredSourceIndices.Count - 1));
            lastIndex = Mathf.Clamp(lastIndex, firstIndex, Mathf.Max(0, _filteredSourceIndices.Count - 1));
        }

        private int FindFirstIndexAtOffset(float offset)
        {
            int low = 0;
            int high = _prefixHeights.Count - 2;
            int result = 0;

            while (low <= high)
            {
                int mid = (low + high) >> 1;
                if (_prefixHeights[mid + 1] > offset)
                {
                    result = mid;
                    high = mid - 1;
                }
                else
                {
                    low = mid + 1;
                }
            }

            return result;
        }

        private int FindLastIndexBeforeOffset(float offset)
        {
            int low = 0;
            int high = _prefixHeights.Count - 2;
            int result = 0;

            while (low <= high)
            {
                int mid = (low + high) >> 1;
                if (_prefixHeights[mid] < offset)
                {
                    result = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return result;
        }

        private void EnsurePoolSize(int visibleCount)
        {
            int targetCount = visibleCount + _poolBuffer;
            while (_pool.Count + _activeItems.Count < targetCount)
            {
                UILogLineItem item = Instantiate(_lineItemPrefab, _content);
                item.gameObject.SetActive(false);
                _pool.Add(item);
            }
        }

        private UILogLineItem GetItemFromPool()
        {
            if (_pool.Count > 0)
            {
                int lastIndex = _pool.Count - 1;
                UILogLineItem item = _pool[lastIndex];
                _pool.RemoveAt(lastIndex);
                return item;
            }

            UILogLineItem created = Instantiate(_lineItemPrefab, _content);
            created.gameObject.SetActive(false);
            return created;
        }

        private void RecycleItem(UILogLineItem item)
        {
            item.BoundIndex = -1;
            item.gameObject.SetActive(false);
            _activeItems.Remove(item);
            _pool.Add(item);
        }

        private void RecycleAllActiveItems()
        {
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                RecycleItem(_activeItems[i]);
            }
        }

        private float GetContentWidth()
        {
            if (_viewport != null)
            {
                return _viewport.rect.width;
            }

            return _content != null ? _content.rect.width : 0f;
        }

        private float GetViewportHeight()
        {
            return _viewport != null ? _viewport.rect.height : 0f;
        }

        private float GetTotalHeight()
        {
            return _prefixHeights.Count > 0 ? _prefixHeights[_prefixHeights.Count - 1] : 0f;
        }

        private float GetScrollOffset()
        {
            return _content != null ? _content.anchoredPosition.y : 0f;
        }

        private void ResetScrollPosition()
        {
            if (_scrollRect == null || _content == null)
            {
                return;
            }

            _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, 0f);
            _scrollRect.verticalNormalizedPosition = 1f;
            _followBottom = true;
        }

        private void ClampScrollPosition()
        {
            if (_scrollRect == null || _content == null)
            {
                return;
            }

            float maxScroll = Mathf.Max(0f, GetTotalHeight() - GetViewportHeight());
            float y = Mathf.Clamp(_content.anchoredPosition.y, 0f, maxScroll);
            _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, y);
            RefreshVisibleItems();
        }

        private void RestoreScrollAfterResize(float previousScrollOffset, float previousTotalHeight, float viewportHeight)
        {
            float maxPreviousScroll = Mathf.Max(0f, previousTotalHeight - viewportHeight);
            float maxCurrentScroll = Mathf.Max(0f, GetTotalHeight() - viewportHeight);
            if (maxPreviousScroll <= 0f || maxCurrentScroll <= 0f)
            {
                ResetScrollPosition();
                return;
            }

            float ratio = previousScrollOffset / maxPreviousScroll;
            float y = ratio * maxCurrentScroll;
            _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, y);
            RefreshVisibleItems();
        }

        private bool IsNearBottom()
        {
            return _scrollRect != null && _scrollRect.verticalNormalizedPosition <= BottomThreshold;
        }

        private void ScrollToBottomImmediate()
        {
            if (_scrollRect == null || _content == null)
            {
                return;
            }

            float maxScroll = Mathf.Max(0f, GetTotalHeight() - GetViewportHeight());
            _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, maxScroll);
            _scrollRect.verticalNormalizedPosition = 0f;
            _followBottom = true;
            RefreshVisibleItems();
        }

        private IEnumerator ScrollToBottomWhenReady()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            ScrollToBottomImmediate();
            _scrollCoroutine = null;
        }
    }
}
