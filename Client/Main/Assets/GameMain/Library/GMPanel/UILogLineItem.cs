using TMPro;
using UnityEngine;

namespace Dragon.Library
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UILogLineItem : MonoBehaviour
    {
        private TextMeshProUGUI _text;
        private RectTransform _rectTransform;

        public RectTransform RectTransform => _rectTransform;
        public int BoundIndex { get; set; } = -1;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            _rectTransform = (RectTransform)transform;
        }

        public void SetText(string text)
        {
            _text.text = text;
        }

        public float MeasureHeight(float width, float minHeight, float verticalPadding)
        {
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            _text.textWrappingMode = TextWrappingModes.Normal;
            _text.overflowMode = TextOverflowModes.Overflow;
            _text.ForceMeshUpdate(true, true);
            return Mathf.Max(minHeight, _text.preferredHeight + verticalPadding);
        }

        public void Layout(float width, float height, float yOffsetFromTop)
        {
            _rectTransform.anchorMin = new Vector2(0f, 1f);
            _rectTransform.anchorMax = new Vector2(1f, 1f);
            _rectTransform.pivot = new Vector2(0.5f, 1f);
            _rectTransform.anchoredPosition = new Vector2(0f, -yOffsetFromTop);
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            _text.ForceMeshUpdate(true, true);
        }
    }
}
