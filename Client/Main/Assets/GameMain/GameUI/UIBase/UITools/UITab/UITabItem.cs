using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dragon.GameUI
{
    /// <summary>
    /// 单个页签按钮的表现组件，由 <see cref="UITabGroup"/> 统一控制选中状态。
    /// </summary>
    [DisallowMultipleComponent]
    public class UITabItem : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private Button _button;
        [SerializeField] private List<GameObject> _selectedObjects = new();
        [SerializeField] private List<GameObject> _unselectedObjects = new();

        #endregion

        #region Properties

        public Button Button
        {
            get
            {
                if (_button == null)
                {
                    _button = GetComponent<Button>();
                }

                return _button;
            }
        }

        #endregion

        #region Public Methods

        public void SetSelected(bool selected)
        {
            SetObjectsActive(_selectedObjects, selected);
            SetObjectsActive(_unselectedObjects, !selected);
        }

        #endregion

        #region Private Methods

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
