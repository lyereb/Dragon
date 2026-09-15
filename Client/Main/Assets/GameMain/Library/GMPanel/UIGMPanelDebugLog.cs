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
        [SerializeField] private TextMeshProUGUI _logText;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Button _clearButton;

        private void Awake()
        {
            if (_clearButton != null)
            {
                _clearButton.onClick.RemoveListener(OnClickClear);
                _clearButton.onClick.AddListener(OnClickClear);
            }
        }

        private void OnDestroy()
        {
            if (_clearButton != null)
            {
                _clearButton.onClick.RemoveListener(OnClickClear);
            }
        }

        private void OnEnable()
        {
            if (GameCenter.GMPanelManager?.DebugLogManager == null)
            {
                return;
            }

            GameCenter.GMPanelManager.DebugLogManager.Changed += OnLogsChanged;
            RefreshLogText(scrollToBottom: true);
        }

        private void OnDisable()
        {
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

        private void OnLogsChanged()
        {
            RefreshLogText(scrollToBottom: false);
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
