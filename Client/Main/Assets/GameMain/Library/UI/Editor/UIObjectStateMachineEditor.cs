#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Dragon.GameUI
{
    [CustomEditor(typeof(UIObjectStateMachine))]
    public class UIObjectStateMachineEditor : UnityEditor.Editor
    {
        private SerializedProperty _initialStateIndexProp;
        private SerializedProperty _statesProp;

        private readonly List<bool> _stateFoldouts = new();
        private bool _showDuplicateNameWarning;

        private void OnEnable()
        {
            _initialStateIndexProp = serializedObject.FindProperty("_initialStateIndex");
            _statesProp = serializedObject.FindProperty("_states");
            SyncFoldoutCount();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SyncFoldoutCount();

            EditorGUILayout.HelpBox(
                "在下方注册状态并为每个状态分配显示对象。同一时刻仅有一个状态生效，其余对象会自动隐藏。\n" +
                "可在编辑模式下点击「预览」查看各状态效果。",
                MessageType.Info);

            DrawStatesSection();
            EditorGUILayout.Space(6f);
            DrawInitialStateSection();
            EditorGUILayout.Space(6f);
            DrawPreviewSection();

            if (_showDuplicateNameWarning)
            {
                EditorGUILayout.HelpBox("存在重复的状态名称，请确保每个状态名称唯一。", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStatesSection()
        {
            EditorGUILayout.LabelField("状态列表", EditorStyles.boldLabel);

            if (_statesProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("尚未注册任何状态，点击下方按钮添加。", MessageType.None);
            }

            for (int i = 0; i < _statesProp.arraySize; i++)
            {
                DrawStateEntry(i);
            }

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("添加状态", GUILayout.Height(24f)))
                {
                    AddState();
                }
            }
        }

        private void DrawStateEntry(int index)
        {
            SerializedProperty entryProp = _statesProp.GetArrayElementAtIndex(index);
            SerializedProperty nameProp = entryProp.FindPropertyRelative("_name");
            SerializedProperty objectsProp = entryProp.FindPropertyRelative("_objects");

            string displayName = string.IsNullOrEmpty(nameProp.stringValue)
                ? $"状态 {index}"
                : nameProp.stringValue;

            EditorGUILayout.BeginVertical(GUI.skin.box);
            using (new EditorGUILayout.HorizontalScope())
            {
                _stateFoldouts[index] = EditorGUILayout.Foldout(_stateFoldouts[index], $"[{index}] {displayName}", true);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("删除", GUILayout.Width(48f)))
                {
                    RemoveState(index);
                    EditorGUILayout.EndVertical();
                    return;
                }
            }

            if (_stateFoldouts[index])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(nameProp, new GUIContent("状态名称"));
                EditorGUILayout.PropertyField(objectsProp, new GUIContent("显示对象"), true);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("添加对象槽位", GUILayout.Height(22f)))
                    {
                        objectsProp.arraySize++;
                    }

                    if (GUILayout.Button("预览此状态", GUILayout.Height(22f)))
                    {
                        serializedObject.ApplyModifiedProperties();
                        PreviewState(index);
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private void DrawInitialStateSection()
        {
            EditorGUILayout.LabelField("初始状态", EditorStyles.boldLabel);

            if (_statesProp.arraySize == 0)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField("初始状态索引", 0);
                }

                return;
            }

            string[] stateOptions = BuildStateOptions(out _showDuplicateNameWarning);
            int currentIndex = Mathf.Clamp(_initialStateIndexProp.intValue, 0, _statesProp.arraySize - 1);
            int newIndex = EditorGUILayout.Popup("初始状态", currentIndex, stateOptions);
            if (newIndex != _initialStateIndexProp.intValue)
            {
                _initialStateIndexProp.intValue = newIndex;
            }
        }

        private void DrawPreviewSection()
        {
            EditorGUILayout.LabelField("编辑模式预览", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(_statesProp.arraySize == 0))
            {
                if (GUILayout.Button("预览初始状态", GUILayout.Height(24f)))
                {
                    serializedObject.ApplyModifiedProperties();
                    int index = Mathf.Clamp(_initialStateIndexProp.intValue, 0, _statesProp.arraySize - 1);
                    PreviewState(index);
                }
            }
        }

        private void AddState()
        {
            int newIndex = _statesProp.arraySize;
            _statesProp.InsertArrayElementAtIndex(newIndex);

            SerializedProperty entryProp = _statesProp.GetArrayElementAtIndex(newIndex);
            entryProp.FindPropertyRelative("_name").stringValue = $"State{newIndex}";
            entryProp.FindPropertyRelative("_objects").ClearArray();

            _stateFoldouts.Add(true);
        }

        private void RemoveState(int index)
        {
            _statesProp.DeleteArrayElementAtIndex(index);
            if (index < _stateFoldouts.Count)
            {
                _stateFoldouts.RemoveAt(index);
            }

            if (_statesProp.arraySize == 0)
            {
                _initialStateIndexProp.intValue = 0;
                return;
            }

            if (_initialStateIndexProp.intValue >= _statesProp.arraySize)
            {
                _initialStateIndexProp.intValue = _statesProp.arraySize - 1;
            }
        }

        private string[] BuildStateOptions(out bool hasDuplicateNames)
        {
            hasDuplicateNames = false;
            var names = new HashSet<string>();
            var options = new string[_statesProp.arraySize];

            for (int i = 0; i < _statesProp.arraySize; i++)
            {
                SerializedProperty nameProp = _statesProp.GetArrayElementAtIndex(i).FindPropertyRelative("_name");
                string name = nameProp.stringValue;
                if (string.IsNullOrEmpty(name))
                {
                    name = $"状态 {i}";
                }

                if (!names.Add(name))
                {
                    hasDuplicateNames = true;
                    name = $"{name} (重复)";
                }

                options[i] = name;
            }

            return options;
        }

        private void PreviewState(int index)
        {
            GameObject[] managedObjects = CollectManagedObjects(_statesProp);
            if (managedObjects.Length > 0)
            {
                Undo.RecordObjects(managedObjects, "Preview UI Object State");
            }

            foreach (Object selected in targets)
            {
                if (selected is not UIObjectStateMachine machine)
                {
                    continue;
                }

                machine.SetState(index, force: true);
                EditorUtility.SetDirty(machine);

                if (!Application.isPlaying && machine.gameObject.scene.IsValid())
                {
                    EditorSceneManager.MarkSceneDirty(machine.gameObject.scene);
                }
            }

            SceneView.RepaintAll();
        }

        private static GameObject[] CollectManagedObjects(SerializedProperty statesProp)
        {
            var objects = new HashSet<GameObject>();
            for (int i = 0; i < statesProp.arraySize; i++)
            {
                SerializedProperty objectsProp = statesProp.GetArrayElementAtIndex(i).FindPropertyRelative("_objects");
                for (int j = 0; j < objectsProp.arraySize; j++)
                {
                    if (objectsProp.GetArrayElementAtIndex(j).objectReferenceValue is GameObject obj)
                    {
                        objects.Add(obj);
                    }
                }
            }

            var result = new GameObject[objects.Count];
            objects.CopyTo(result);
            return result;
        }

        private void SyncFoldoutCount()
        {
            while (_stateFoldouts.Count < _statesProp.arraySize)
            {
                _stateFoldouts.Add(true);
            }

            while (_stateFoldouts.Count > _statesProp.arraySize)
            {
                _stateFoldouts.RemoveAt(_stateFoldouts.Count - 1);
            }
        }
    }
}
#endif
