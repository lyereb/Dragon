using TMPro;
using UnityEngine;

namespace Dragon.Localization
{
    /// <summary>
    /// 代码侧一行绑定本地化 TMP，无需手动订阅 LanguageChanged。
    /// </summary>
    public static class LocalizationHelper
    {
        public static void SetText(TextMeshProUGUI tmp, string key)
        {
            if (tmp == null)
                return;

            GetOrAddHandler(tmp).SetText(key);
        }

        public static void SetText(TextMeshPro tmp, string key)
        {
            if (tmp == null)
                return;

            GetOrAddHandler(tmp).SetText(key);
        }

        public static void SetText(TextMeshProUGUI tmp, LocalString localString)
        {
            if (tmp == null)
                return;

            GetOrAddHandler(tmp).SetText(localString);
        }

        private static LocalizationUIHandler GetOrAddHandler(Component textComponent)
        {
            LocalizationUIHandler handler = textComponent.GetComponent<LocalizationUIHandler>();
            if (handler == null)
                handler = textComponent.gameObject.AddComponent<LocalizationUIHandler>();

            return handler;
        }
    }
}
