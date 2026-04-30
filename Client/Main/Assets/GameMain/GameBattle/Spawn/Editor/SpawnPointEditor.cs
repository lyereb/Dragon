using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpawnPoint))]
public class SpawnPointEditor : Editor
{
    private SerializedProperty _entityTypeProp;
    private SerializedProperty _characterTypeProp;
    private SerializedProperty _interactableTypeProp;
    private SerializedProperty _configIdProp;

    private void OnEnable()
    {
        _entityTypeProp = serializedObject.FindProperty("_entityType");
        _characterTypeProp = serializedObject.FindProperty("_characterType");
        _interactableTypeProp = serializedObject.FindProperty("_interactableType");
        _configIdProp = serializedObject.FindProperty("_configId");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_entityTypeProp);

        EntityType entityType = (EntityType)_entityTypeProp.enumValueIndex;
        if (entityType == EntityType.Character)
        {
            EditorGUILayout.PropertyField(_characterTypeProp);
        }
        else if (entityType == EntityType.Interactable)
        {
            EditorGUILayout.PropertyField(_interactableTypeProp);
        }

        EditorGUILayout.PropertyField(_configIdProp);

        serializedObject.ApplyModifiedProperties();
    }
}
