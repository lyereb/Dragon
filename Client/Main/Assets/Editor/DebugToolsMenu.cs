#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Dragon.Library.Editor
{
    public static class DebugToolsMenu
    {
        private const string ProductionMenuPath = "Tools/Debug/Production Environment";

        [MenuItem(ProductionMenuPath, false, 100)]
        private static void ToggleProductionEnvironment()
        {
            GameBuildSetting setting = GameBuildSetting.GetOrCreateAsset();
            bool next = !setting.IsProductionValue;
            setting.SetIsProduction(next);
            Debug.Log(next
                ? $"[Tools] 已切换为正式环境，已写入 {GameBuildSetting.AssetPath}（下次打包生效）"
                : $"[Tools] 已切换为测试环境，已写入 {GameBuildSetting.AssetPath}（下次打包生效）");
        }

        [MenuItem(ProductionMenuPath, true)]
        private static bool ToggleProductionEnvironmentValidate()
        {
            var setting = AssetDatabase.LoadAssetAtPath<GameBuildSetting>(GameBuildSetting.AssetPath);
            Menu.SetChecked(ProductionMenuPath, setting != null && setting.IsProductionValue);
            return true;
        }

        [MenuItem("Tools/Debug/Clear PlayerPrefs", false, 200)]
        private static void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[Tools] PlayerPrefs cleared.");
        }
    }
}
#endif
