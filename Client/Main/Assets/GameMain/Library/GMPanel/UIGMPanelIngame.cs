using Dragon.Localization;
using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// GM 面板 InGame 页，随 Tab 内容显隐；由 <see cref="GameUI.UITabGroup"/> 控制激活。
    /// </summary>
    public class UIGMPanelIngame : MonoBehaviour
    {
        /// <summary>调试：在简体中文与英文之间切换。</summary>
        public void OnToggleLanguage()
        {
            LocalSettingManager settings = GameCenter.LocalSettingManager;
            LanguageType nextLanguage = settings.Language == LanguageType.English
                ? LanguageType.SimplifiedChinese
                : LanguageType.English;
            settings.SetLanguage(nextLanguage);
            Debug.Log($"[Debug] Language switched to {nextLanguage}");
            GameCenter.GMPanelManager?.CloseGMPanel();
        }
    }
}
