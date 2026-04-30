using System;
using System.Collections.Generic;
using Dragon.Config;
using cfg.dragon;

namespace Dragon.Localization
{
    public enum LanguageType
    {
        SimplifiedChinese,
        TraditionalChinese,
        English,
        Japanese,
        Korean,
    }

    public class LocalManager
    {
        #region //静态变量
        //静态变量
        protected static LocalManager _instance = null;
        //静态属性
        public static LocalManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LocalManager();
                }
                return _instance;
            }
        }
        #endregion


        public LanguageType CurrentLanguage { get; private set; } = LanguageType.SimplifiedChinese;

        public Action LanguageChanged;

        public void Init()
        {

        }

        public void SwitchLanguage(LanguageType language)
        {
            CurrentLanguage = language;
            LanguageChanged?.Invoke();
        }

        public string GetLocalString(string key)
        {
            if (!int.TryParse(key, out int languageId))
            {
                return key;
            }

            // 配置表初始化未完成特殊处理
            if (ConfigManager.Instance.Tables == null || ConfigManager.Instance.Tables.Tblocalization == null)
            {
                return null;
            }

            localization cfg = ConfigManager.Instance.Tables?.Tblocalization?.Get(languageId);

            if (cfg != null)
            {
                switch (CurrentLanguage)
                {
                    case LanguageType.SimplifiedChinese:
                        return cfg.SimplifiedChinese;
                    case LanguageType.TraditionalChinese:
                        return cfg.TraditionalChinese;
                    case LanguageType.English:
                        return cfg.English;
                    case LanguageType.Japanese:
                        return cfg.Japanese;
                    case LanguageType.Korean:
                        return cfg.Korean;
                }
            }

            return key;
        }
    }
}