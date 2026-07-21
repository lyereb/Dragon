#if UNITY_EDITOR
using System.Collections.Generic;
using Dragon.Localization;
using SimpleJSON;
using UnityEditor;
using UnityEngine;

namespace Dragon.Localization.Editor
{
    internal static class LocalStringEditorPreview
    {
        private const string LocalizationJsonPath = "Assets/AddressableAssets/Configs/dragon_tblocalization.json";
        private const string PreviewLanguagePrefKey = "LocalStringUIHandler.PreviewLanguage";

        private static Dictionary<int, JSONNode> _entriesById;
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

            if (!TryLoadTable(out Dictionary<int, JSONNode> entries))
                return key;

            if (!entries.TryGetValue(languageId, out JSONNode entry))
                return key;

            string fieldName = LocalizationLanguageMapper.GetFieldName(language);
            JSONNode valueNode = entry[fieldName];
            return valueNode != null && valueNode.IsString ? valueNode.Value : key;
        }

        private static bool TryLoadTable(out Dictionary<int, JSONNode> entries)
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

            _entriesById = new Dictionary<int, JSONNode>();
            _cachedJsonGuid = assetGuid;

            JSONNode root = JSON.Parse(jsonAsset.text);
            if (root == null || !root.IsArray)
                return false;

            foreach (JSONNode item in root.AsArray)
            {
                if (item == null || !item.HasKey("id"))
                    continue;

                _entriesById[item["id"].AsInt] = item;
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
