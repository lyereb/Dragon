#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using Dragon.Config;

namespace Dragon.Resource
{
    /// <summary>
    /// Addressable资源工具类
    /// </summary>
    public static class AddressableAssetUtility
    {
        /// <summary>
        /// 检查资产是否已经是Addressable
        /// </summary>
        public static bool IsAssetAddressable(string assetPath)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return false;

            var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));
            return entry != null;
        }

        /// <summary>
        /// 将资产标记为Addressable
        /// </summary>
        public static void MarkAssetAsAddressable(string assetPath, string address = null)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressable设置未找到，请先初始化Addressable系统");
                return;
            }

            // 获取或创建配置组
            var configGroup = GetOrCreateConfigGroup(settings);
            
            // 添加资产到Addressable系统
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.CreateOrMoveEntry(guid, configGroup, false, false);
            
            // 设置自定义地址
            if (!string.IsNullOrEmpty(address))
            {
                entry.SetAddress(address);
            }
            
            // 添加标签
            entry.SetLabel(ConfigLoadConst.CONFIG_ADDRESSABLE_LABEL, true, true);
        }

        /// <summary>
        /// 移除资产的Addressable标记
        /// </summary>
        public static void RemoveAssetFromAddressable(string assetPath)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.FindAssetEntry(guid);
            
            if (entry != null)
            {
                settings.RemoveAssetEntry(entry.guid, false);
            }
        }

        /// <summary>
        /// 获取或创建配置专用组
        /// </summary>
        private static AddressableAssetGroup GetOrCreateConfigGroup(
            AddressableAssetSettings settings)
        {
            var configGroupName = "ConfigGroup";
            var group = settings.FindGroup(configGroupName);
            
            if (group == null)
            {
                group = settings.CreateGroup(configGroupName, false, false, false, 
                    settings.DefaultGroup.Schemas);
            }
            
            return group;
        }

        /// <summary>
        /// 扫描配置目录获取所有配置文件
        /// </summary>
        public static string[] ScanConfigFiles()
        {
            if (!System.IO.Directory.Exists(ConfigLoadConst.CONFIG_FOLDER_PATH))
            {
                Debug.LogWarning($"配置目录不存在: {ConfigLoadConst.CONFIG_FOLDER_PATH}");
                return new string[0];
            }

            var configFiles = System.IO.Directory.GetFiles(
                ConfigLoadConst.CONFIG_FOLDER_PATH, 
                "*" + ConfigLoadConst.CONFIG_FILE_EXTENSION);
                
            var fileNames = new string[configFiles.Length];
            for (int i = 0; i < configFiles.Length; i++)
            {
                fileNames[i] = System.IO.Path.GetFileNameWithoutExtension(configFiles[i]);
            }
            
            System.Array.Sort(fileNames);
            return fileNames;
        }
    }
}
#endif