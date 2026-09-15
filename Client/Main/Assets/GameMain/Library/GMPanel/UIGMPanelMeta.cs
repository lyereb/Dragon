using Dragon.GameUI;
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
    }
}
