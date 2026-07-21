#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Dragon.Library.Editor
{
    public static class DebugToolsMenu
    {
        [MenuItem("Tools/Debug/Clear PlayerPrefs")]
        private static void ClearPlayerPrefs()
        {
            if (!EditorUtility.DisplayDialog(
                    "Clear PlayerPrefs",
                    "确定清空当前项目在编辑器中的全部 PlayerPrefs？\n（含教程已读标记）",
                    "清空",
                    "取消"))
            {
                return;
            }

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[Tools] PlayerPrefs cleared.");
        }
    }
}
#endif
