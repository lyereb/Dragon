using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dragon.GameUI
{
    [Serializable]
    public class UIObjectStateEntry
    {
        [SerializeField] private string _name;
        [SerializeField] private List<GameObject> _objects = new();

        public string Name => _name;
        public IReadOnlyList<GameObject> Objects => _objects;

        public UIObjectStateEntry()
        {
        }

        public UIObjectStateEntry(string name, IEnumerable<GameObject> objects)
        {
            _name = name;
            _objects = objects != null ? new List<GameObject>(objects) : new List<GameObject>();
        }
    }

    /// <summary>
    /// 通用 UI 对象状态机。挂载在 UI 根节点上，通过状态列表配置各状态下需要显示的对象。
    /// 同一时刻仅有一个状态生效，其余状态的对象全部隐藏。
    /// </summary>
    [DisallowMultipleComponent]
    public class UIObjectStateMachine : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private int _initialStateIndex;
        [SerializeField] private List<UIObjectStateEntry> _states = new();

        #endregion

        #region Properties

        public int CurrentIndex { get; private set; } = -1;
        public string CurrentStateName { get; private set; }
        public int StateCount => _states?.Count ?? 0;

        #endregion

        #region Unity Lifecycle

        private void Reset()
        {
            _initialStateIndex = 0;
            _states = new List<UIObjectStateEntry>
            {
                new UIObjectStateEntry("State0", new List<GameObject>()),
                new UIObjectStateEntry("State1", new List<GameObject>()),
            };
        }

        private void Awake()
        {
            ApplyInitialState();
        }

        #endregion

        #region Public Methods

        /// <summary>切换到指定索引的状态。</summary>
        public bool SetState(int index, bool force = false)
        {
            if (!IsValidIndex(index))
            {
                Debug.LogWarning($"{nameof(UIObjectStateMachine)}: 无效状态索引 {index}，节点={name}");
                return false;
            }

            if (!force && CurrentIndex == index)
            {
                return false;
            }

            ApplyState(index);
            return true;
        }

        /// <summary>切换到指定名称的状态，可绑定到 UnityEvent(string)。</summary>
        public bool SetState(string stateName, bool force = false)
        {
            int index = FindIndexByName(stateName);
            if (index < 0)
            {
                Debug.LogWarning($"{nameof(UIObjectStateMachine)}: 未找到状态 \"{stateName}\"，节点={name}");
                return false;
            }

            return SetState(index, force);
        }

        /// <summary>切换到下一个状态，末尾后回到第一个；可绑定到 Button.onClick。</summary>
        public bool NextState()
        {
            if (StateCount == 0)
            {
                return false;
            }

            int nextIndex = CurrentIndex < 0 ? 0 : (CurrentIndex + 1) % StateCount;
            return SetState(nextIndex);
        }

        /// <summary>在相邻状态间切换；仅有两个状态时可绑定到 Button.onClick。</summary>
        public bool ToggleState()
        {
            if (StateCount != 2)
            {
                return NextState();
            }

            return SetState(CurrentIndex == 0 ? 1 : 0);
        }

        /// <summary>运行时注册新状态。</summary>
        public bool RegisterState(string stateName, IEnumerable<GameObject> objects)
        {
            if (string.IsNullOrEmpty(stateName))
            {
                Debug.LogWarning($"{nameof(UIObjectStateMachine)}: 状态名称不能为空，节点={name}");
                return false;
            }

            if (FindIndexByName(stateName) >= 0)
            {
                Debug.LogWarning($"{nameof(UIObjectStateMachine)}: 状态 \"{stateName}\" 已存在，节点={name}");
                return false;
            }

            _states ??= new List<UIObjectStateEntry>();
            _states.Add(new UIObjectStateEntry(stateName, objects));
            return true;
        }

        /// <summary>按名称移除已注册的状态。</summary>
        public bool UnregisterState(string stateName)
        {
            int index = FindIndexByName(stateName);
            if (index < 0)
            {
                return false;
            }

            return UnregisterState(index);
        }

        /// <summary>按索引移除已注册的状态。</summary>
        public bool UnregisterState(int index)
        {
            if (!IsValidIndex(index))
            {
                return false;
            }

            _states.RemoveAt(index);

            if (StateCount == 0)
            {
                CurrentIndex = -1;
                CurrentStateName = null;
                return true;
            }

            if (CurrentIndex == index)
            {
                int fallbackIndex = Mathf.Clamp(index, 0, StateCount - 1);
                ApplyState(fallbackIndex);
            }
            else if (CurrentIndex > index)
            {
                CurrentIndex--;
            }

            return true;
        }

        /// <summary>按初始索引应用默认状态。</summary>
        public bool ApplyInitialState()
        {
            if (StateCount == 0)
            {
                CurrentIndex = -1;
                CurrentStateName = null;
                return false;
            }

            int index = IsValidIndex(_initialStateIndex) ? _initialStateIndex : 0;
            ApplyState(index);
            return true;
        }

        #endregion

        #region Private Methods

        private bool IsValidIndex(int index)
        {
            return _states != null && index >= 0 && index < _states.Count;
        }

        private int FindIndexByName(string stateName)
        {
            if (_states == null || string.IsNullOrEmpty(stateName))
            {
                return -1;
            }

            for (int i = 0; i < _states.Count; i++)
            {
                UIObjectStateEntry entry = _states[i];
                if (entry != null && entry.Name == stateName)
                {
                    return i;
                }
            }

            return -1;
        }

        private void ApplyState(int index)
        {
            for (int i = 0; i < _states.Count; i++)
            {
                UIObjectStateEntry entry = _states[i];
                if (entry == null)
                {
                    continue;
                }

                SetObjectsActive(entry.Objects, i == index);
            }

            CurrentIndex = index;
            CurrentStateName = _states[index]?.Name;
        }

        private static void SetObjectsActive(IReadOnlyList<GameObject> objects, bool active)
        {
            if (objects == null)
            {
                return;
            }

            for (int i = 0; i < objects.Count; i++)
            {
                GameObject target = objects[i];
                if (target != null)
                {
                    target.SetActive(active);
                }
            }
        }

        #endregion
    }
}
