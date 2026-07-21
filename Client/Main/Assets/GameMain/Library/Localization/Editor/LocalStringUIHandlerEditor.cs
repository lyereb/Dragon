#if UNITY_EDITOR
using Dragon.Localization.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Dragon.Localization.Editor
{
    [CustomEditor(typeof(LocalStringUIHandler))]
    public class LocalStringUIHandlerEditor : UnityEditor.Editor
    {
        private SerializedProperty _localizationKeyProp;

        [InitializeOnLoadMethod]
        private static void RegisterEditorRefreshCallback()
        {
            LocalStringUIHandler.EditorRefreshCallback = RefreshHandler;
        }

        private void OnEnable()
        {
            _localizationKeyProp = serializedObject.FindProperty("_localizationKey");
            TryMigrateKeyFromTextComponent();
            RefreshHandler((LocalStringUIHandler)target);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "填写语言表 ID（数字），编辑器会根据下方预览语言自动刷新 TMP 文本。运行时以 Localization Key 为准。",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_localizationKeyProp, new GUIContent("Localization Key"));

            LanguageType previewLanguage = LocalStringEditorPreview.PreviewLanguage;
            LanguageType newPreviewLanguage = (LanguageType)EditorGUILayout.EnumPopup("Preview Language", previewLanguage);
            if (newPreviewLanguage != previewLanguage)
            {
                LocalStringEditorPreview.PreviewLanguage = newPreviewLanguage;
            }

            bool changed = EditorGUI.EndChangeCheck();
            if (serializedObject.ApplyModifiedProperties() || changed)
            {
                foreach (Object selected in targets)
                {
                    if (selected is LocalStringUIHandler handler)
                        RefreshHandler(handler);
                }
            }

            DrawPreviewStatus();
        }

        private void DrawPreviewStatus()
        {
            var handler = (LocalStringUIHandler)target;
            string key = handler.LocalizationKey;
            if (string.IsNullOrEmpty(key))
                return;

            LanguageType language = LocalStringEditorPreview.PreviewLanguage;
            string previewText = LocalStringEditorPreview.GetPreviewText(key, language);
            bool missingEntry = int.TryParse(key, out _) && previewText == key;

            EditorGUILayout.Space(6f);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Preview Text", previewText);
            }

            if (missingEntry)
            {
                EditorGUILayout.HelpBox(
                    $"未在 dragon_tblocalization.json 中找到 ID={key}，Scene 中将显示 Key 本身。",
                    MessageType.Warning);
            }
        }

        private void TryMigrateKeyFromTextComponent()
        {
            var handler = (LocalStringUIHandler)target;
            if (!string.IsNullOrEmpty(handler.LocalizationKey))
                return;

            string sourceText = GetSourceText(handler);
            if (string.IsNullOrEmpty(sourceText))
                return;

            Undo.RecordObject(handler, "Migrate Localization Key");
            handler.LocalizationKey = sourceText;
            EditorUtility.SetDirty(handler);
        }

        private static void RefreshHandler(LocalStringUIHandler handler)
        {
            if (handler == null || Application.isPlaying)
                return;

            string previewText = LocalStringEditorPreview.GetPreviewText(
                handler.LocalizationKey,
                LocalStringEditorPreview.PreviewLanguage);

            Undo.RecordObject(handler, "Refresh Localization Preview");

            var textMeshProUGUI = handler.GetComponent<TextMeshProUGUI>();
            if (textMeshProUGUI != null)
            {
                Undo.RecordObject(textMeshProUGUI, "Refresh Localization Preview");
                textMeshProUGUI.text = previewText;
                EditorUtility.SetDirty(textMeshProUGUI);
            }
            else
            {
                var textMeshPro = handler.GetComponent<TextMeshPro>();
                if (textMeshPro != null)
                {
                    Undo.RecordObject(textMeshPro, "Refresh Localization Preview");
                    textMeshPro.text = previewText;
                    EditorUtility.SetDirty(textMeshPro);
                }
            }

            SceneView.RepaintAll();
        }

        private static string GetSourceText(LocalStringUIHandler handler)
        {
            var textMeshProUGUI = handler.GetComponent<TextMeshProUGUI>();
            if (textMeshProUGUI != null && !string.IsNullOrEmpty(textMeshProUGUI.text))
                return textMeshProUGUI.text;

            var textMeshPro = handler.GetComponent<TextMeshPro>();
            if (textMeshPro != null && !string.IsNullOrEmpty(textMeshPro.text))
                return textMeshPro.text;

            return null;
        }
    }
}
#endif
