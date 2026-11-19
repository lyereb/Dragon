#if UNITY_EDITOR
using System.Collections.Generic;
using Dragon.Localization;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Dragon.Localization.Editor
{
    internal static class LocalStringEditorPreview
    {
        private const string LocalizationJsonPath = "Assets/AddressableAssets/Configs/dragon_tblocalization.json";
        private const string PreviewLanguagePrefKey = "LocalizationUIHandler.PreviewLanguage";

        private static Dictionary<int, JObject> _entriesById;
        private static string _cachedJsonGuid;

        public static LanguageType PreviewLanguage
        {
            get => (LanguageType)EditorPrefs.GetInt(PreviewLanguagePrefKey, (int)LanguageType.SimplifiedChinese);
            set => EditorPrefs.SetInt(PreviewLanguagePrefKey, (int)value);
        }

        public static string GetPreviewText(string key, LanguageType language)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            if (!int.TryParse(key, out int languageId))
                return key;

            if (!TryLoadTable(out Dictionary<int, JObject> entries))
                return key;

            if (!entries.TryGetValue(languageId, out JObject entry))
                return key;

            JToken valueNode = entry[LocalizationLanguageMapper.GetFieldName(language)];
            return valueNode != null && valueNode.Type == JTokenType.String ? valueNode.Value<string>() : key;
        }

        private static bool TryLoadTable(out Dictionary<int, JObject> entries)
        {
            entries = _entriesById;
            TextAsset jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(LocalizationJsonPath);
            if (jsonAsset == null)
            {
                _entriesById = null;
                _cachedJsonGuid = null;
                return false;
            }

            string assetGuid = AssetDatabase.AssetPathToGUID(LocalizationJsonPath);
            if (_entriesById != null && _cachedJsonGuid == assetGuid)
                return true;

            _entriesById = new Dictionary<int, JObject>();
            _cachedJsonGuid = assetGuid;

            if (JToken.Parse(jsonAsset.text) is not JArray rows)
                return false;

            foreach (JToken item in rows)
            {
                if (item is not JObject row)
                    continue;

                JToken idNode = row["id"];
                if (idNode == null || idNode.Type == JTokenType.Null)
                    continue;

                _entriesById[idNode.Value<int>()] = row;
            }

            entries = _entriesById;
            return true;
        }

        public static void InvalidateCache()
        {
            _entriesById = null;
            _cachedJsonGuid = null;
        }
    }
}
#endif
