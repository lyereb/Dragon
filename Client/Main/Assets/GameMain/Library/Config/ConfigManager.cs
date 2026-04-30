using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using cfg;
using SimpleJSON;
using UnityEngine;
using Dragon.Resource;

namespace Dragon.Config
{
    public class ConfigManager
    {
        #region //静态变量

        //静态变量
        protected static ConfigManager _instance = null;

        //静态属性
        public static ConfigManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ConfigManager();
                }
                return _instance;
            }
        }
        #endregion

        public Tables Tables { get; private set; }
        public ConfigFileList ConfigFileList { get; private set; }

        public Action<float> OnLoadProgress;

        private Dictionary<string, JSONNode> _configData = new Dictionary<string, JSONNode>();
        private int _loadedCount = 0;
        private int _totalCount = 0;

        public bool IsInitialized => Tables != null;

        private async UniTask LoadConfigsAsync()
        {
            try
            {
                // 第一步：加载配置文件列表
                await LoadConfigFileListAsync();
                
                if (ConfigFileList == null || !ConfigFileList.IsValid())
                {
                    Debug.LogError("配置文件列表加载失败或无效");
                    return;
                }

                // 第二步：加载配置数据
                await LoadConfigDataAsync();

                // 第三步：初始化Tables
                InitializeTables();
            }
            catch (Exception ex)
            {
                Debug.LogError($"配置加载过程中发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步加载配置文件列表
        /// </summary>
        private async UniTask LoadConfigFileListAsync()
        {
            UniTaskCompletionSource<ConfigFileList> tcs = new UniTaskCompletionSource<ConfigFileList>();
            
            // 直接使用回调，避免CS4014警告
            _ = ResourceManager.Instance.LoadAsync<ConfigFileList>(
                ConfigLoadConst.CONFIG_FILE_LIST_PATH,
                (configList) => {
                    if (configList != null)
                    {
                        ConfigFileList = configList;
                        _totalCount = ConfigFileList.GetConfigCount();
                        Debug.Log($"配置文件列表加载完成，共{_totalCount}个配置文件");
                        tcs.TrySetResult(ConfigFileList);
                    }
                    else
                    {
                        Debug.LogError($"加载配置文件列表失败: 未找到配置文件 {ConfigLoadConst.CONFIG_FILE_LIST_PATH}");
                        tcs.TrySetResult(null);
                    }
                });

            await tcs.Task;
        }

        /// <summary>
        /// 异步加载配置数据
        /// </summary>
        private async UniTask LoadConfigDataAsync()
        {
            if (ConfigFileList == null) return;

            _loadedCount = 0;
            var configNames = ConfigFileList.GetConfigArray();

            // 使用UniTask.WhenAll并行加载所有配置文件
            var loadTasks = new List<UniTask>();
            foreach (string fileName in configNames)
            {
                loadTasks.Add(LoadSingleConfigAsync(fileName));
            }
            
            // 等待所有配置文件加载完成
            await UniTask.WhenAll(loadTasks);
        }

        /// <summary>
        /// 异步加载单个配置文件
        /// </summary>
        private async UniTask LoadSingleConfigAsync(string fileName)
        {
            UniTaskCompletionSource<TextAsset> tcs = new UniTaskCompletionSource<TextAsset>();
            
            // 根据加载格式构建地址
            string address = ConfigLoadConst.CONFIG_ADDRESSABLE_PREFIX + fileName;
            
            _ = ResourceManager.Instance.LoadAsync<TextAsset>(
                address,
                (asset) => {
                    if (asset != null)
                    {
                        try
                        {
                            JSONNode jsonData = JSON.Parse(asset.text);
                            _configData[fileName] = jsonData;
                            Debug.Log($"配置文件加载成功: {fileName}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"解析配置文件失败 {fileName}: {ex.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"加载配置文件失败 {fileName}: 未找到资源 {address}");
                    }
                    
                    // 更新进度
                    _loadedCount++;
                    float progress = (float)_loadedCount / _totalCount;
                    OnLoadProgress?.Invoke(progress);
                    
                    tcs.TrySetResult(asset);
                });

            await tcs.Task;
        }

        /// <summary>
        /// 初始化Tables
        /// </summary>
        private void InitializeTables()
        {
            Tables = new Tables(fileName => {
                if (_configData.TryGetValue(fileName, out JSONNode jsonData))
                {
                    return jsonData;
                }
                Debug.LogError($"找不到配置数据: {fileName}");
                return null;
            });
            
            Debug.Log("ConfigManager初始化完成");
        }

        public async UniTask Init()
        {            
            // 使用UniTask启动异步加载
            await LoadConfigsAsync();
        }
    }
}