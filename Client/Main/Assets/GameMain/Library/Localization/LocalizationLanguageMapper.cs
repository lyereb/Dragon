using System;
using System.Collections.Generic;
using System.Reflection;
using cfg.dragon;
using UnityEngine;

namespace Dragon.Localization
{
    /// <summary>
    /// LanguageType 枚举名须与 Luban localization bean 字段名一致（PascalCase）。
    /// 新增语言只需扩展 LanguageType 并在 Luban 表加同名列。
    /// </summary>
    public static class LocalizationLanguageMapper
    {
        private static readonly Dictionary<LanguageType, FieldInfo> FieldByLanguage;

        static LocalizationLanguageMapper()
        {
            FieldByLanguage = new Dictionary<LanguageType, FieldInfo>();
            Type localizationType = typeof(localization);

            foreach (LanguageType language in Enum.GetValues(typeof(LanguageType)))
            {
                FieldInfo field = localizationType.GetField(language.ToString());
                if (field != null && field.FieldType == typeof(string))
                {
                    FieldByLanguage[language] = field;
                    continue;
                }

                Debug.LogWarning(
                    $"[Localization] LanguageType.{language} has no matching string field on cfg.dragon.localization.");
            }
        }

        public static string GetFieldName(LanguageType language) => language.ToString();

        public static bool TryGetLocalizedText(localization cfg, LanguageType language, out string text)
        {
            text = null;
            if (cfg == null || !FieldByLanguage.TryGetValue(language, out FieldInfo field))
                return false;

            text = field.GetValue(cfg) as string;
            return true;
        }
    }
}
