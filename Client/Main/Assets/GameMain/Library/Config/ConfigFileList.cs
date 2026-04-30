using System.Collections.Generic;
using UnityEngine;

namespace Dragon.Config
{
    /// <summary>
    /// 配置文件列表SO - 简化版本
    /// </summary>
    [CreateAssetMenu(fileName = "ConfigFileList", menuName = "Game/Config File List")]
    public class ConfigFileList : ScriptableObject
    {
        [Header("配置加载格式")]
        [Tooltip("配置文件加载格式（如json,bytes等）")]
        public string loadFormat = "json";
        
        [Header("配置文件列表")]
        [Tooltip("需要加载的配置文件名称列表（不包含扩展名）")]
        public List<string> configFileNames = new List<string>();

        /// <summary>
        /// 验证配置列表有效性
        /// </summary>
        public bool IsValid()
        {
            return configFileNames != null && configFileNames.Count > 0;
        }
        
        /// <summary>
        /// 获取配置文件数量
        /// </summary>
        public int GetConfigCount()
        {
            return configFileNames?.Count ?? 0;
        }
        
        /// <summary>
        /// 清空配置列表
        /// </summary>
        public void Clear()
        {
            configFileNames.Clear();
        }
        
        /// <summary>
        /// 添加配置文件
        /// </summary>
        public void AddConfig(string fileName)
        {
            if (!configFileNames.Contains(fileName))
            {
                configFileNames.Add(fileName);
            }
        }
        
        /// <summary>
        /// 移除配置文件
        /// </summary>
        public bool RemoveConfig(string fileName)
        {
            return configFileNames.Remove(fileName);
        }
        
        /// <summary>
        /// 获取配置文件数组
        /// </summary>
        public string[] GetConfigArray()
        {
            return configFileNames?.ToArray() ?? new string[0];
        }
    }
}