using UnityEngine;
using UnityEngine.UI;

namespace Dragon.Library
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class UIGMPanelEntrance : MonoBehaviour
    {
        public void OnClickOpenGMPanel()
        {
            GameCenter.GMPanelManager?.OpenGMPanel();
        }
    }
}
