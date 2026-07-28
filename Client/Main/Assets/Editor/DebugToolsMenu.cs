#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Dragon.Library.Editor
{
    public static class DebugToolsMenu
    {
        private const string ProductionMenuPath = "Tools/Debug/Production Environment";
        private const string EnableUnityLogMenuPath = "Tools/Debug/Enable Unity Log";

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

        [MenuItem(EnableUnityLogMenuPath, false, 110)]
        private static void ToggleEnableUnityLog()
        {
            GameBuildSetting setting = GameBuildSetting.GetOrCreateAsset();
            bool next = !setting.EnableUnityLogValue;
            // 开启前先临时打开 logger，否则关闭时的 Log 会被吞掉
            if (next)
            {
                Debug.unityLogger.logEnabled = true;
            }

            setting.SetEnableUnityLog(next);
            if (next)
            {
                Debug.Log($"[Tools] 已开启 Unity Log，已写入 {GameBuildSetting.AssetPath}");
            }
            else
            {
                Debug.unityLogger.logEnabled = true;
                Debug.Log($"[Tools] 已关闭 Unity Log，已写入 {GameBuildSetting.AssetPath}");
                Debug.unityLogger.logEnabled = false;
            }
        }

        [MenuItem(EnableUnityLogMenuPath, true)]
        private static bool ToggleEnableUnityLogValidate()
        {
            var setting = AssetDatabase.LoadAssetAtPath<GameBuildSetting>(GameBuildSetting.AssetPath);
            Menu.SetChecked(EnableUnityLogMenuPath, setting == null || setting.EnableUnityLogValue);
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
