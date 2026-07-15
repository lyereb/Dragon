#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Dragon.GameUI
{
    [CustomEditor(typeof(UITMPOutlineEffect))]
    public class UITMPOutlineEffectEditor : UnityEditor.Editor
    {
        private SerializedProperty _enableOutlineProp;
        private SerializedProperty _outlineColorProp;
        private SerializedProperty _outlineWidthProp;
        private SerializedProperty _outlineSoftnessProp;
        private SerializedProperty _overrideFaceColorProp;
        private SerializedProperty _faceColorProp;
        private SerializedProperty _faceDilateProp;
        private SerializedProperty _enableUnderlayProp;
        private SerializedProperty _underlayColorProp;
        private SerializedProperty _underlayOffsetXProp;
        private SerializedProperty _underlayOffsetYProp;
        private SerializedProperty _underlayDilateProp;
        private SerializedProperty _underlaySoftnessProp;

        private void OnEnable()
        {
            _enableOutlineProp = serializedObject.FindProperty("_enableOutline");
            _outlineColorProp = serializedObject.FindProperty("_outlineColor");
            _outlineWidthProp = serializedObject.FindProperty("_outlineWidth");
            _outlineSoftnessProp = serializedObject.FindProperty("_outlineSoftness");
            _overrideFaceColorProp = serializedObject.FindProperty("_overrideFaceColor");
            _faceColorProp = serializedObject.FindProperty("_faceColor");
            _faceDilateProp = serializedObject.FindProperty("_faceDilate");
            _enableUnderlayProp = serializedObject.FindProperty("_enableUnderlay");
            _underlayColorProp = serializedObject.FindProperty("_underlayColor");
            _underlayOffsetXProp = serializedObject.FindProperty("_underlayOffsetX");
            _underlayOffsetYProp = serializedObject.FindProperty("_underlayOffsetY");
            _underlayDilateProp = serializedObject.FindProperty("_underlayDilate");
            _underlaySoftnessProp = serializedObject.FindProperty("_underlaySoftness");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "挂载在与 TMP_Text 相同的 GameObject 上，修改参数后 Scene 视图即时预览。",
                MessageType.Info);

            DrawOutlineSection();
            EditorGUILayout.Space(6f);
            DrawFaceSection();
            EditorGUILayout.Space(6f);
            DrawUnderlaySection();

            EditorGUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(!Application.isPlaying && !target))
            {
                if (GUILayout.Button("立即刷新预览", GUILayout.Height(24f)))
                {
                    ApplyPreview();
                }
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                ApplyPreview();
            }
        }

        private void DrawOutlineSection()
        {
            EditorGUILayout.LabelField("描边", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_enableOutlineProp, new GUIContent("启用描边"));

            using (new EditorGUI.DisabledScope(!_enableOutlineProp.boolValue))
            {
                EditorGUILayout.PropertyField(_outlineColorProp, new GUIContent("描边颜色"));
                EditorGUILayout.PropertyField(_outlineWidthProp, new GUIContent("描边宽度"));
                EditorGUILayout.PropertyField(_outlineSoftnessProp, new GUIContent("描边柔化"));
            }
        }

        private void DrawFaceSection()
        {
            EditorGUILayout.LabelField("填充", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_faceDilateProp, new GUIContent("填充扩张"));
            EditorGUILayout.PropertyField(_overrideFaceColorProp, new GUIContent("覆盖文字颜色"));

            using (new EditorGUI.DisabledScope(!_overrideFaceColorProp.boolValue))
            {
                EditorGUILayout.PropertyField(_faceColorProp, new GUIContent("文字颜色"));
            }
        }

        private void DrawUnderlaySection()
        {
            EditorGUILayout.LabelField("底层投影（Underlay）", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_enableUnderlayProp, new GUIContent("启用 Underlay"));

            using (new EditorGUI.DisabledScope(!_enableUnderlayProp.boolValue))
            {
                EditorGUILayout.PropertyField(_underlayColorProp, new GUIContent("颜色"));
                EditorGUILayout.PropertyField(_underlayOffsetXProp, new GUIContent("X 偏移"));
                EditorGUILayout.PropertyField(_underlayOffsetYProp, new GUIContent("Y 偏移"));
                EditorGUILayout.PropertyField(_underlayDilateProp, new GUIContent("扩张"));
                EditorGUILayout.PropertyField(_underlaySoftnessProp, new GUIContent("柔化"));
            }
        }

        private void ApplyPreview()
        {
            foreach (Object selected in targets)
            {
                if (selected is UITMPOutlineEffect effect)
                {
                    effect.ApplyEffect();
                    EditorUtility.SetDirty(effect);
                }
            }

            SceneView.RepaintAll();
        }
    }
}
#endif
