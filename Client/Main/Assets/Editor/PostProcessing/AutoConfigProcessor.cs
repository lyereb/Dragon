using System.IO;
using UnityEditor;
using UnityEngine;
using Dragon.Resource;
using Dragon.Config;

namespace Dragon.Editor
{
    /// <summary>
    /// 自动配置处理器 - EditorWindow版本
    /// 处理配置文件扫描和配置生成
    /// </summary>
    public class AutoConfigProcessor : EditorWindow
    {
        private Vector2 _scrollPosition;
        private string[] _currentConfigFiles = new string[0];
        private bool _autoRefresh = true;
        private float _lastRefreshTime = 0f;
        private const float REFRESH_INTERVAL = 2f;

        // 修改资源存储路径，使用Addressable兼容的路径
        private const string CONFIG_FILE_LIST_ASSET_PATH = ConfigLoadConst.CONFIG_FILE_LIST_STORAGE_PATH;

        [MenuItem("Tools/Config/Auto Config Processor")]
        public static void ShowWindow()
        {
            var window = GetWindow<AutoConfigProcessor>("配置处理器");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshConfigList();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawControls();
            DrawConfigList();
            DrawFooter();
            
            // 自动刷新
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > REFRESH_INTERVAL)
            {
                RefreshConfigList();
                _lastRefreshTime = (float)EditorApplication.timeSinceStartup;
            }
        }

        private void DrawHeader()
        {
            GUILayout.Label("配置文件自动处理器", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "此工具自动扫描配置目录，生成配置文件列表，并处理Addressable标记。",
                MessageType.Info);
        }

        private void DrawControls()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("🔄 刷新配置列表", GUILayout.Height(30)))
            {
                RefreshConfigList();
            }
            
            if (GUILayout.Button("⚙️ 生成配置列表", GUILayout.Height(30)))
            {
                GenerateConfigFileList();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("📦 标记为Addressable", GUILayout.Height(30)))
            {
                MarkAllAsAddressable();
            }
            
            if (GUILayout.Button("📋 验证配置", GUILayout.Height(30)))
            {
                ValidateConfigs();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 自动刷新选项
            _autoRefresh = EditorGUILayout.ToggleLeft("自动刷新 (2秒间隔)", _autoRefresh);
            
            EditorGUILayout.Space();
        }

        private void DrawConfigList()
        {
            GUILayout.Label($"配置文件列表 (共{_currentConfigFiles.Length}个)", EditorStyles.boldLabel);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            if (_currentConfigFiles.Length == 0)
            {
                EditorGUILayout.HelpBox("未找到配置文件，请检查配置目录路径。", MessageType.Warning);
            }
            else
            {
                foreach (string fileName in _currentConfigFiles)
                {
                    DrawConfigItem(fileName);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawConfigItem(string fileName)
        {
            var assetPath = $"{ConfigLoadConst.CONFIG_FOLDER_PATH}/{fileName}{ConfigLoadConst.CONFIG_FILE_EXTENSION}";
            var isAddressable = AddressableAssetUtility.IsAssetAddressable(assetPath);
            
            EditorGUILayout.BeginHorizontal();
            
            // 状态图标
            var statusIcon = isAddressable ? "✅" : "❌";
            GUILayout.Label(statusIcon, GUILayout.Width(20));
            
            // 文件名
            EditorGUILayout.LabelField(fileName, GUILayout.ExpandWidth(true));
            
            // 操作按钮
            if (!isAddressable)
            {
                if (GUILayout.Button("标记", GUILayout.Width(60)))
                {
                    var address = ConfigLoadConst.CONFIG_ADDRESSABLE_PREFIX + fileName;
                    AddressableAssetUtility.MarkAssetAsAddressable(assetPath, address);
                    AssetDatabase.SaveAssets();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space();
            
            GUILayout.Label("工具信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"配置目录: {ConfigLoadConst.CONFIG_FOLDER_PATH}");
            EditorGUILayout.LabelField($"加载格式: json");
            EditorGUILayout.LabelField($"Addressable前缀: {ConfigLoadConst.CONFIG_ADDRESSABLE_PREFIX}");
        }

        /// <summary>
        /// 刷新配置文件列表
        /// </summary>
        private void RefreshConfigList()
        {
            _currentConfigFiles = AddressableAssetUtility.ScanConfigFiles();
            Repaint();
        }

        /// <summary>
        /// 生成配置文件列表资源
        /// </summary>
        private void GenerateConfigFileList()
        {
            if (_currentConfigFiles.Length == 0)
            {
                Debug.LogWarning("没有找到配置文件，无法生成列表");
                return;
            }

            // 获取或创建ConfigFileList
            var configFileList = GetOrCreateConfigFileList();
            
            // 更新配置列表
            configFileList.Clear();
            configFileList.loadFormat = "json";
            
            foreach (string fileName in _currentConfigFiles)
            {
                configFileList.AddConfig(fileName);
            }
            
            // 保存资源
            EditorUtility.SetDirty(configFileList);
            AssetDatabase.SaveAssets();
            
            // 标记为Addressable
            MarkConfigFileListAsAddressable();
            
            Debug.Log($"✅ 配置文件列表生成完成，共{configFileList.GetConfigCount()}个配置文件");
        }

        /// <summary>
        /// 标记所有配置文件为Addressable
        /// </summary>
        private void MarkAllAsAddressable()
        {
            int markedCount = 0;
            
            foreach (string fileName in _currentConfigFiles)
            {
                var assetPath = $"{ConfigLoadConst.CONFIG_FOLDER_PATH}/{fileName}{ConfigLoadConst.CONFIG_FILE_EXTENSION}";
                
                if (!AddressableAssetUtility.IsAssetAddressable(assetPath))
                {
                    var address = ConfigLoadConst.CONFIG_ADDRESSABLE_PREFIX + fileName;
                    AddressableAssetUtility.MarkAssetAsAddressable(assetPath, address);
                    markedCount++;
                }
            }
            
            // 同时标记ConfigFileList为Addressable
            MarkConfigFileListAsAddressable();
            
            if (markedCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"✅ 标记了 {markedCount} 个配置文件为Addressable");
            }
            else
            {
                Debug.Log("所有配置文件已经是Addressable资源");
            }
        }

        /// <summary>
        /// 标记ConfigFileList为Addressable资源
        /// </summary>
        private void MarkConfigFileListAsAddressable()
        {
            if (AddressableAssetUtility.IsAssetAddressable(CONFIG_FILE_LIST_ASSET_PATH))
                return;
                
            AddressableAssetUtility.MarkAssetAsAddressable(CONFIG_FILE_LIST_ASSET_PATH, ConfigLoadConst.CONFIG_FILE_LIST_PATH);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 验证配置完整性
        /// </summary>
        private void ValidateConfigs()
        {
            int validCount = 0;
            int invalidCount = 0;
            
            foreach (string fileName in _currentConfigFiles)
            {
                var assetPath = $"{ConfigLoadConst.CONFIG_FOLDER_PATH}/{fileName}{ConfigLoadConst.CONFIG_FILE_EXTENSION}";
                
                if (File.Exists(assetPath) && AddressableAssetUtility.IsAssetAddressable(assetPath))
                {
                    validCount++;
                }
                else
                {
                    invalidCount++;
                    Debug.LogWarning($"配置文件存在问题: {fileName}");
                }
            }
            
            // 验证ConfigFileList
            if (AddressableAssetUtility.IsAssetAddressable(CONFIG_FILE_LIST_ASSET_PATH))
            {
                validCount++;
            }
            else
            {
                invalidCount++;
                Debug.LogWarning("ConfigFileList资源未标记为Addressable");
            }
            
            Debug.Log($"✅ 配置验证完成 - 有效: {validCount}, 无效: {invalidCount}");
        }

        /// <summary>
        /// 获取或创建ConfigFileList资源
        /// </summary>
        private ConfigFileList GetOrCreateConfigFileList()
        {
             // 在Editor环境下使用AssetDatabase.LoadAssetAtPath替代Resources.Load
            var configFileList = AssetDatabase.LoadAssetAtPath<ConfigFileList>(CONFIG_FILE_LIST_ASSET_PATH);
            
            if (configFileList == null)
            {
                configFileList = CreateInstance<ConfigFileList>();
                
                // 确保安全目录存在
                var directory = Path.GetDirectoryName(CONFIG_FILE_LIST_ASSET_PATH);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    // 创建.meta文件避免被忽略
                    var metaContent = "fileFormatVersion: 2\nguid: " + GUID.Generate() + "\nfolderAsset: yes\n";
                    File.WriteAllText(directory + ".meta", metaContent);
                }
                
                // 保存到安全目录下
                AssetDatabase.CreateAsset(configFileList, CONFIG_FILE_LIST_ASSET_PATH);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            
            return configFileList;
        }

        [MenuItem("Tools/Config/Quick Process")]
        public static void QuickProcess()
        {
            var processor = CreateInstance<AutoConfigProcessor>();
            processor.RefreshConfigList();
            processor.GenerateConfigFileList();
            processor.MarkAllAsAddressable();
            DestroyImmediate(processor);
            Debug.Log("✅ 快速处理完成");
        }

        /// <summary>
        /// 打开仓库 Config/Datas 目录（相对工程根：../../Config/Datas）
        /// </summary>
        [MenuItem("Tools/Config/Open Config Datas")]
        public static void OpenConfigDatasFolder()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var configDatasPath = Path.GetFullPath(Path.Combine(projectRoot, "..", "..", "Config", "Datas"));

            if (!Directory.Exists(configDatasPath))
            {
                EditorUtility.DisplayDialog("打开失败", $"Config Datas 目录不存在:\n{configDatasPath}", "确定");
                return;
            }

            // Windows / macOS 分别用系统命令直接进入目录（RevealInFinder 只会在父目录选中）
#if UNITY_EDITOR_OSX
            System.Diagnostics.Process.Start("open", configDatasPath);
#else
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = configDatasPath,
                UseShellExecute = true
            });
#endif
        }
    }
}