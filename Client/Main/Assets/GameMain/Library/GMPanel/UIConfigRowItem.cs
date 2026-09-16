using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dragon.Library
{
    public class UIConfigRowItem : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI _idText;
        [SerializeField] private GameObject _selectedIndicator;

        public int Index { get; private set; }

        private Action<int> _onClick;

        public void Bind(int index, string idDisplay, Action<int> onClick)
        {
            Index = index;
            _onClick = onClick;

            if (_idText != null)
            {
                _idText.text = idDisplay;
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (_selectedIndicator != null)
            {
                _selectedIndicator.SetActive(selected);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _onClick?.Invoke(Index);
        }
    }
}
