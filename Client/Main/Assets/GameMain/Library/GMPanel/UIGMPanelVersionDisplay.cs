using TMPro;
using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// GM 面板版本号显示；打开面板时读取 <see cref="Application.version"/>（Player Settings → Version）。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UIGMPanelVersionDisplay : MonoBehaviour
    {
        private TextMeshProUGUI _text;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (_text == null)
            {
                return;
            }

            _text.text = $"v{Application.version}";
        }
    }
}
