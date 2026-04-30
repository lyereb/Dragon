#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Dragon.Resource;
using Dragon.Config;

namespace Dragon.Editor
{
    /// <summary>
    /// Luban配置导出后处理器
    /// 通知AutoConfigProcessor处理配置文件变化
    /// </summary>
    public class LubanPostProcessor : AssetPostprocessor
    {
        /// <summary>
        /// 资源导入完成后调用
        /// </summary>
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, 
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool configChanged = false;
            
            // 检查导入的资产
            foreach (string assetPath in importedAssets)
            {
                if (IsConfigAsset(assetPath))
                {
                    HandleConfigChange(assetPath, "导入");
                    configChanged = true;
                }
            }
            
            // 检查删除的资产
            foreach (string assetPath in deletedAssets)
            {
                if (IsConfigAsset(assetPath))
                {
                    HandleConfigChange(assetPath, "删除");
                    configChanged = true;
                }
            }
            
            // 如果配置有变化，通知AutoConfigProcessor更新
            if (configChanged)
            {
                NotifyConfigProcessor();
            }
        }

        /// <summary>
        /// 判断是否为配置资产
        /// </summary>
        private static bool IsConfigAsset(string assetPath)
        {
            return assetPath.StartsWith(ConfigLoadConst.CONFIG_FOLDER_PATH) && 
                   assetPath.EndsWith(ConfigLoadConst.CONFIG_FILE_EXTENSION);
        }

        /// <summary>
        /// 处理配置文件变化
        /// </summary>
        private static void HandleConfigChange(string assetPath, string operation)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            Debug.Log($"检测到配置文件{operation}: {fileName}");
            
            // 自动处理Addressable标记
            if (operation == "导入" && !AddressableAssetUtility.IsAssetAddressable(assetPath))
            {
                var address = ConfigLoadConst.CONFIG_ADDRESSABLE_PREFIX + fileName;
                AddressableAssetUtility.MarkAssetAsAddressable(assetPath, address);
                Debug.Log($"自动标记为Addressable: {fileName}");
            }
            else if (operation == "删除" && AddressableAssetUtility.IsAssetAddressable(assetPath))
            {
                AddressableAssetUtility.RemoveAssetFromAddressable(assetPath);
                Debug.Log($"移除Addressable标记: {fileName}");
            }
        }

        /// <summary>
        /// 通知配置处理器更新
        /// </summary>
        private static void NotifyConfigProcessor()
        {
            // 延迟调用以确保AssetDatabase操作完成
            EditorApplication.delayCall += () => {
                // 触发快速处理
                AutoConfigProcessor.QuickProcess();
            };
        }
    }
}
#endif