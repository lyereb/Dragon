using Dragon.Library;
using Dragon.Logic;
using TMPro;
using UnityEngine;

namespace Dragon.GameUI
{
    /// <summary>
    /// 红点 UI 挂载：只负责显隐与计数展示，业务计算在 Logic/RedDot。
    /// </summary>
    public class RedDotView : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField] private GameObject _dotRoot;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private RedDotDisplayStyle _style = RedDotDisplayStyle.DotOrCount;
        [SerializeField] private int _maxDisplayCount = 99;

        private bool _eventRegistered;

        public string Key => _key;

        private void OnEnable()
        {
            RegisterEvent();
            Refresh();
        }

        private void OnDisable()
        {
            UnregisterEvent();
        }

        /// <summary>运行时修改绑定的 key（如动态 Tab）。</summary>
        public void SetKey(string key)
        {
            _key = key;
            Refresh();
        }

        public void Refresh()
        {
            if (string.IsNullOrEmpty(_key) || RedDotManager.Instance == null)
            {
                ApplyDisplay(RedDotValue.None);
                return;
            }

            ApplyDisplay(RedDotManager.Instance.GetValue(_key));
        }

        private void RegisterEvent()
        {
            if (_eventRegistered)
            {
                return;
            }

            EventManager.Instance.Register(UIEventDefine.EID_REDDOT_CHANGED, OnRedDotChanged);
            _eventRegistered = true;
        }

        private void UnregisterEvent()
        {
            if (!_eventRegistered)
            {
                return;
            }

            EventManager.Instance.UnRegister(UIEventDefine.EID_REDDOT_CHANGED, OnRedDotChanged);
            _eventRegistered = false;
        }

        private void OnRedDotChanged(object param, object sender)
        {
            if (param is not RedDotChangedEventParam changed)
            {
                Refresh();
                return;
            }

            if (ShouldRefreshForChangedKey(changed.Key))
            {
                Refresh();
            }
        }

        private bool ShouldRefreshForChangedKey(string changedKey)
        {
            if (string.IsNullOrEmpty(_key) || string.IsNullOrEmpty(changedKey))
            {
                return true;
            }

            if (changedKey == _key)
            {
                return true;
            }

            // 子节点变化会影响父节点聚合展示
            return changedKey.StartsWith(_key + ".") || _key.StartsWith(changedKey + ".");
        }

        private void ApplyDisplay(RedDotValue value)
        {
            bool visible = value.Visible;
            int count = value.Count;

            if (!visible)
            {
                SetDotActive(false);
                SetCountActive(false);
                return;
            }

            switch (_style)
            {
                case RedDotDisplayStyle.DotOnly:
                    SetDotActive(true);
                    SetCountActive(false);
                    break;

                case RedDotDisplayStyle.CountOnly:
                    SetDotActive(false);
                    SetCountActive(true);
                    SetCountText(count);
                    break;

                case RedDotDisplayStyle.DotAndCount:
                    SetDotActive(true);
                    SetCountActive(true);
                    SetCountText(count);
                    break;

                case RedDotDisplayStyle.DotOrCount:
                default:
                    if (count <= 1)
                    {
                        SetDotActive(true);
                        SetCountActive(false);
                    }
                    else
                    {
                        SetDotActive(true);
                        SetCountActive(true);
                        SetCountText(count);
                    }

                    break;
            }
        }

        private void SetDotActive(bool active)
        {
            if (_dotRoot != null)
            {
                _dotRoot.SetActive(active);
            }
        }

        private void SetCountActive(bool active)
        {
            if (_countText != null)
            {
                _countText.gameObject.SetActive(active);
            }
        }

        private void SetCountText(int count)
        {
            if (_countText == null)
            {
                return;
            }

            _countText.text = count > _maxDisplayCount
                ? _maxDisplayCount + "+"
                : count.ToString();
        }
    }
}
