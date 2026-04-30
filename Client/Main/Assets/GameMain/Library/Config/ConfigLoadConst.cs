namespace Dragon.Config
{
    /// <summary>
    /// 配置模块通用常量
    /// </summary>
    public static class ConfigLoadConst
    {
        /// <summary>
        /// 配置文件目录路径
        /// </summary>
        public const string CONFIG_FOLDER_PATH = "Assets/AddressableAssets/Configs";

        /// <summary>
        /// ConfigFileList安全存储路径（不会被鲁班工具影响）
        /// </summary>
        public const string CONFIG_FILE_LIST_STORAGE_PATH = "Assets/AddressableAssets/Data/ConfigFileList.asset";
        
        /// <summary>
        /// 配置文件列表资源路径
        /// </summary>
        public const string CONFIG_FILE_LIST_PATH = "ConfigFileList";
        
        /// <summary>
        /// 配置文件扩展名
        /// </summary>
        public const string CONFIG_FILE_EXTENSION = ".json";
        
        /// <summary>
        /// 配置文件Addressable标签
        /// </summary>
        public const string CONFIG_ADDRESSABLE_LABEL = "Config";
        
        /// <summary>
        /// 配置文件Addressable地址前缀
        /// </summary>
        public const string CONFIG_ADDRESSABLE_PREFIX = "Configs/";
    }
}