using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dragon.Library
{
    /// <summary>
    /// GM 面板内的 Debug 日志视图，随 Tab 内容显隐；由 <see cref="UITabGroup"/> 控制激活。
    /// </summary>
    public class UIGMPanelDebugLog : MonoBehaviour
    {
        private const float BottomThreshold = 0.02f;

        [SerializeField] private TextMeshProUGUI _logText;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Button _clearButton;

        private bool _followBottom = true;
        private Coroutine _scrollCoroutine;

        private void Awake()
        {
            if (_clearButton != null)
            {
                _clearButton.onClick.RemoveListener(OnClickClear);
                _clearButton.onClick.AddListener(OnClickClear);
            }

            if (_scrollRect != null)
            {
                _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
                _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
            }
        }

        private void OnDestroy()
        {
            if (_clearButton != null)
            {
                _clearButton.onClick.RemoveListener(OnClickClear);
            }

            if (_scrollRect != null)
            {
                _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            }
        }

        private void OnEnable()
        {
            if (GameCenter.GMPanelManager?.DebugLogManager == null)
            {
                return;
            }

            _followBottom = true;
            GameCenter.GMPanelManager.DebugLogManager.Changed += OnLogsChanged;
            RefreshLogText(scrollToBottom: true);
        }

        private void OnDisable()
        {
            if (_scrollCoroutine != null)
            {
                StopCoroutine(_scrollCoroutine);
                _scrollCoroutine = null;
            }

            if (GameCenter.GMPanelManager?.DebugLogManager == null)
            {
                return;
            }

            GameCenter.GMPanelManager.DebugLogManager.Changed -= OnLogsChanged;
        }

        public void OnClickClear()
        {
            GameCenter.GMPanelManager?.DebugLogManager?.Clear();
        }

        private void OnScrollValueChanged(Vector2 _)
        {
            if (_scrollRect == null)
            {
                return;
            }

            _followBottom = _scrollRect.verticalNormalizedPosition <= BottomThreshold;
        }

        private void OnLogsChanged()
        {
            RefreshLogText(scrollToBottom: _followBottom);
        }

        private void RefreshLogText(bool scrollToBottom)
        {
            if (_logText == null || GameCenter.GMPanelManager?.DebugLogManager == null)
            {
                return;
            }

            _logText.text = GameCenter.GMPanelManager.DebugLogManager.GetFullText();

            if (scrollToBottom)
            {
                RequestScrollToBottom();
            }
        }

        private void RequestScrollToBottom()
        {
            if (_scrollCoroutine != null)
            {
                StopCoroutine(_scrollCoroutine);
            }

            _scrollCoroutine = StartCoroutine(ScrollToBottomWhenReady());
        }

        private IEnumerator ScrollToBottomWhenReady()
        {
            yield return null;

            if (_logText != null)
            {
                _logText.ForceMeshUpdate();
            }

            if (_scrollRect?.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.content);
            }

            Canvas.ForceUpdateCanvases();
            yield return null;

            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 0f;
                _followBottom = true;
            }

            _scrollCoroutine = null;
        }
    }
}
