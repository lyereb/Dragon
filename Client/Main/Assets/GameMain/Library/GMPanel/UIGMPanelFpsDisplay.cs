using TMPro;
using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// GM 面板左上角 FPS 显示；面板打开期间按 unscaled 时间平滑采样。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UIGMPanelFpsDisplay : MonoBehaviour
    {
        private const float SampleInterval = 0.5f;

        private TextMeshProUGUI _text;
        private float _accumulatedTime;
        private int _frameCount;
        private float _fps;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            _accumulatedTime = 0f;
            _frameCount = 0;
            _fps = 0f;
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
            _accumulatedTime = 0f;
            _frameCount = 0;
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (_text == null)
            {
                return;
            }

            _text.text = _fps > 0f ? _fps.ToString("F1") : "-";
        }
    }
}
