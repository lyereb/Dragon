using Dragon.GameUI;
using Dragon.Localization;
using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// GM 面板 Meta 页，随 Tab 内容显隐；由 <see cref="GameUI.UITabGroup"/> 控制激活。
    /// </summary>
    public class UIGMPanelMeta : MonoBehaviour
    {
        public void OnTestTut()
        {
            PopupManager.Instance.OpenPopup(
                "popup_tutorial_template",
                new PopupTutorialSlideData());
        }

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

        /// <summary>调试：清空全部 PlayerPrefs 并重载本地设置。</summary>
        public void OnClearPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            if (GameCenter.LocalSettingManager != null)
            {
                GameCenter.LocalSettingManager.Clear();
                GameCenter.LocalSettingManager.Init();
            }

            Debug.Log("[Debug] PlayerPrefs cleared.");
        }
    }
}
