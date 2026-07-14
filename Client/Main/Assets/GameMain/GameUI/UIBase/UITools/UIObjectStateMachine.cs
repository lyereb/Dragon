using System.Collections.Generic;
using UnityEngine;

namespace Dragon.GameUI
{
    public enum UIObjectStateType
    {
        /// <summary>折叠状态。</summary>
        Collapsed,

        /// <summary>展开状态。</summary>
        Expanded,
    }

    /// <summary>
    /// 通用 UI 对象状态机。挂载在 UI 根节点上，通过两组对象列表在两种状态间切换显示。
    /// 将 <see cref="ToggleState"/> 绑定到 Button.onClick 即可切换。
    /// </summary>
    [DisallowMultipleComponent]
    public class UIObjectStateMachine : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private UIObjectStateType _initialState = UIObjectStateType.Collapsed;
        [SerializeField] private List<GameObject> _collapsedObjects = new();
        [SerializeField] private List<GameObject> _expandedObjects = new();

        #endregion

        #region Properties

        public UIObjectStateType CurrentState => _currentState;

        #endregion

        #region Private Fields

        private UIObjectStateType _currentState;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ApplyState(_initialState);
        }

        #endregion

        #region Public Methods

        /// <summary>在两种状态之间切换，可绑定到 Button.onClick。</summary>
        public void ToggleState()
        {
            ApplyState(_currentState == UIObjectStateType.Collapsed
                ? UIObjectStateType.Expanded
                : UIObjectStateType.Collapsed);
        }

        public void SetCollapsed()
        {
            ApplyState(UIObjectStateType.Collapsed);
        }

        public void SetExpanded()
        {
            ApplyState(UIObjectStateType.Expanded);
        }

        #endregion

        #region Private Methods

        private void ApplyState(UIObjectStateType state)
        {
            _currentState = state;
            bool isCollapsed = state == UIObjectStateType.Collapsed;
            SetObjectsActive(_collapsedObjects, isCollapsed);
            SetObjectsActive(_expandedObjects, !isCollapsed);
        }

        private static void SetObjectsActive(List<GameObject> objects, bool active)
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
