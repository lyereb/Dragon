using System.Collections;
using Dragon.GameUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dragon.Library
{
    public class PopupDebugLogData : PopupBaseData
    {
    }

    public class PopupDebugLog : PopupBase<PopupDebugLogData>
    {
        [SerializeField] private TextMeshProUGUI _logText;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _clearButton;

        private void Awake()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnClickClose);
                _closeButton.onClick.AddListener(OnClickClose);
            }

            if (_clearButton != null)
            {
                _clearButton.onClick.RemoveListener(OnClickClear);
                _clearButton.onClick.AddListener(OnClickClear);
            }
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnClickClose);
            }

            if (_clearButton != null)
            {
                _clearButton.onClick.RemoveListener(OnClickClear);
            }
        }

        private void OnEnable()
        {
            if (GameCenter.DebugLogManager == null)
            {
                return;
            }

            GameCenter.DebugLogManager.Changed += OnLogsChanged;
            RefreshLogText(scrollToBottom: true);
        }

        private void OnDisable()
        {
            if (GameCenter.DebugLogManager == null)
            {
                return;
            }

            GameCenter.DebugLogManager.Changed -= OnLogsChanged;
        }

        protected override void OnOpen(PopupDebugLogData data)
        {
            RefreshLogText(scrollToBottom: true);
        }

        public void OnClickClose()
        {
            Close();
        }

        public void OnClickClear()
        {
            GameCenter.DebugLogManager?.Clear();
        }

        private void OnLogsChanged()
        {
            RefreshLogText(scrollToBottom: false);
        }

        private void RefreshLogText(bool scrollToBottom)
        {
            if (_logText == null || GameCenter.DebugLogManager == null)
            {
                return;
            }

            _logText.text = GameCenter.DebugLogManager.GetFullText();

            if (scrollToBottom)
            {
                StartCoroutine(ScrollToBottomNextFrame());
            }
        }

        private IEnumerator ScrollToBottomNextFrame()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();

            if (_scrollRect == null)
            {
                yield break;
            }

            _scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
