using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Dragon.GameUI
{
    [Serializable]
    public class UITabEntry
    {
        [SerializeField] private UITabItem _tab;
        [SerializeField] private GameObject _content;

        public UITabItem Tab => _tab;
        public GameObject Content => _content;
    }

    /// <summary>
    /// 通用页签组。页签索引由 Entries 在 Inspector 中的配置顺序决定，从 0 开始。
    /// </summary>
    [DisallowMultipleComponent]
    public class UITabGroup : MonoBehaviour
    {
        private sealed class ButtonBinding
        {
            public Button Button;
            public UnityAction Listener;
        }

        #region Serialized Fields

        [SerializeField] private int _defaultIndex;
        [SerializeField] private bool _notifyOnInitialize;
        [SerializeField] private List<UITabEntry> _entries = new();
        [SerializeField] private UnityEvent<int> _onTabChanged = new();

        #endregion

        #region Properties

        public int CurrentIndex { get; private set; } = -1;
        public int EntryCount => _entries?.Count ?? 0;

        #endregion

        #region Private Fields

        private readonly List<ButtonBinding> _buttonBindings = new();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            BindButtons();
        }

        private void Start()
        {
            if (CurrentIndex < 0)
            {
                SelectDefault(_notifyOnInitialize);
            }
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        #endregion

        #region Public Methods

        public bool Select(int index, bool notify = true)
        {
            if (!IsValidIndex(index))
            {
                Debug.LogWarning($"{nameof(UITabGroup)}: 无效页签索引 {index}，节点={name}");
                return false;
            }

            if (CurrentIndex == index)
            {
                return false;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                UITabEntry entry = _entries[i];
                if (entry == null)
                {
                    continue;
                }

                bool selected = i == index;
                entry.Tab?.SetSelected(selected);
                if (entry.Content != null)
                {
                    entry.Content.SetActive(selected);
                }
            }

            CurrentIndex = index;
            if (notify)
            {
                _onTabChanged?.Invoke(index);
            }

            return true;
        }

        public bool SelectDefault(bool notify = true)
        {
            if (EntryCount == 0)
            {
                return false;
            }

            int index = IsValidIndex(_defaultIndex) ? _defaultIndex : 0;
            return Select(index, notify);
        }

        public void AddTabChangedListener(UnityAction<int> listener)
        {
            if (listener == null)
            {
                return;
            }

            _onTabChanged ??= new UnityEvent<int>();
            _onTabChanged.AddListener(listener);
        }

        public void RemoveTabChangedListener(UnityAction<int> listener)
        {
            if (listener == null || _onTabChanged == null)
            {
                return;
            }

            _onTabChanged.RemoveListener(listener);
        }

        #endregion

        #region Private Methods

        private bool IsValidIndex(int index)
        {
            return _entries != null && index >= 0 && index < _entries.Count;
        }

        private void BindButtons()
        {
            UnbindButtons();
            if (_entries == null)
            {
                return;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                UITabEntry entry = _entries[i];
                Button button = entry?.Tab?.Button;
                if (button == null)
                {
                    continue;
                }

                int index = i;
                UnityAction listener = () => Select(index);
                button.onClick.AddListener(listener);
                _buttonBindings.Add(new ButtonBinding
                {
                    Button = button,
                    Listener = listener,
                });
            }
        }

        private void UnbindButtons()
        {
            for (int i = 0; i < _buttonBindings.Count; i++)
            {
                ButtonBinding binding = _buttonBindings[i];
                if (binding?.Button != null)
                {
                    binding.Button.onClick.RemoveListener(binding.Listener);
                }
            }

            _buttonBindings.Clear();
        }

        #endregion
    }
}
