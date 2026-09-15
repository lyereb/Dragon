using UnityEngine;
using UnityEngine.UI;

namespace Dragon.Library
{
    /// <summary>
    /// GM 面板内的 Debug 日志视图，随 Tab 内容显隐；由 <see cref="UITabGroup"/> 控制激活。
    /// </summary>
    public class UIGMPanelDebugLog : MonoBehaviour
    {
        [SerializeField] private UIVirtualLogList _virtualLogList;
        [SerializeField] private Button _clearButton;

        private DebugLogFilter _filter = DebugLogFilter.All;

        private void Awake()
        {
            if (_clearButton != null)
            {
                _clearButton.onClick.RemoveListener(OnClickClear);
                _clearButton.onClick.AddListener(OnClickClear);
            }
        }

        private void OnDestroy()
        {
            if (_clearButton != null)
            {
                _clearButton.onClick.RemoveListener(OnClickClear);
            }
        }

        private void OnEnable()
        {
            DebugLogManager manager = GameCenter.GMPanelManager?.DebugLogManager;
            if (manager == null || _virtualLogList == null)
            {
                return;
            }

            manager.Changed += OnLogsChanged;
            _virtualLogList.Initialize(manager);
            _virtualLogList.SetFilter(_filter, scrollToBottom: true);
        }

        private void OnDisable()
        {
            DebugLogManager manager = GameCenter.GMPanelManager?.DebugLogManager;
            if (manager == null)
            {
                return;
            }

            manager.Changed -= OnLogsChanged;
        }

        public void OnClickClear()
        {
            GameCenter.GMPanelManager?.DebugLogManager?.Clear();
        }

        public void ShowAll()
        {
            SetFilter(DebugLogFilter.All);
        }

        public void ShowLog()
        {
            SetFilter(DebugLogFilter.Log);
        }

        public void ShowWarning()
        {
            SetFilter(DebugLogFilter.Warning);
        }

        public void ShowError()
        {
            SetFilter(DebugLogFilter.Error);
        }

        private void OnLogsChanged(DebugLogChangeKind changeKind)
        {
            if (_virtualLogList == null)
            {
                return;
            }

            _virtualLogList.HandleChange(changeKind, _virtualLogList.FollowBottom);
        }

        private void SetFilter(DebugLogFilter filter)
        {
            if (_filter == filter || _virtualLogList == null)
            {
                return;
            }

            _filter = filter;
            _virtualLogList.SetFilter(_filter, scrollToBottom: true);
        }
    }
}
