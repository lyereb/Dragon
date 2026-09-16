using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dragon.Library
{
    /// <summary>
    /// GM 面板 Diagnostics 页，周期性展示运行时诊断信息。
    /// </summary>
    public class UIGMPanelDiagnostics : MonoBehaviour
    {
        private const float SampleInterval = 0.5f;

        [SerializeField] private TextMeshProUGUI _bodyText;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Button _copyButton;

        private float _accumulatedTime;
        private int _frameCount;
        private float _fps;
        private float _frameTimeMs;
        private string _latestReport = string.Empty;

        private void Awake()
        {
            if (_copyButton != null)
            {
                _copyButton.onClick.RemoveListener(OnCopyDebugInfo);
                _copyButton.onClick.AddListener(OnCopyDebugInfo);
            }
        }

        private void OnDestroy()
        {
            if (_copyButton != null)
            {
                _copyButton.onClick.RemoveListener(OnCopyDebugInfo);
            }
        }

        private void OnEnable()
        {
            _accumulatedTime = 0f;
            _frameCount = 0;
            _fps = 0f;
            _frameTimeMs = 0f;
            RefreshDisplay();
        }

        private void Update()
        {
            _accumulatedTime += Time.unscaledDeltaTime;
            _frameCount++;

            if (_accumulatedTime < SampleInterval)
            {
                return;
            }

            _fps = _frameCount / _accumulatedTime;
            _frameTimeMs = _accumulatedTime / _frameCount * 1000f;
            _accumulatedTime = 0f;
            _frameCount = 0;
            RefreshDisplay();
        }

        public void OnCopyDebugInfo()
        {
            if (string.IsNullOrEmpty(_latestReport))
            {
                RefreshDisplay();
            }

            GUIUtility.systemCopyBuffer = _latestReport;
            Debug.Log("[GM] Diagnostics copied to clipboard.");
        }

        private void RefreshDisplay()
        {
            float fps = _fps;
            float frameTimeMs = _frameTimeMs;
            if (fps <= 0f && Time.unscaledDeltaTime > 0f)
            {
                frameTimeMs = Time.unscaledDeltaTime * 1000f;
                fps = 1f / Time.unscaledDeltaTime;
            }

            _latestReport = GMDiagnosticsCollector.BuildReport(fps, frameTimeMs);
            if (_bodyText != null)
            {
                _bodyText.text = _latestReport;
            }

            if (_scrollRect?.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.content);
            }
        }
    }
}
