using Dragon.GameUI;
using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// 通用 GM 面板管理器，聚合调试日志等 GM 子模块。
    /// 需在 <see cref="GameCenter"/> 中 Init / Shutdown。
    /// </summary>
    public class GMPanelManager
    {
        public const string PopupAddress = "popup_gm_panel";

        private static GMPanelManager _instance;

        public static GMPanelManager Instance => _instance ??= new GMPanelManager();

        public DebugLogManager DebugLogManager { get; private set; }

        public void OpenGMPanel()
        {
            GameCenter.PopupManager.OpenPopup<PopupGMPanelData>(PopupAddress, new PopupGMPanelData());
        }

        public void CloseGMPanel()
        {
            if (GameCenter.PopupManager.TryGetOpenedPopup(PopupAddress, out PopupGMPanel popup))
            {
                popup.Close();
            }
        }

        public void Init()
        {
            DebugLogManager = DebugLogManager.Instance;
            DebugLogManager.Init();
        }

        public void Shutdown()
        {
            DebugLogManager?.Shutdown();
            DebugLogManager = null;
        }
    }
}
