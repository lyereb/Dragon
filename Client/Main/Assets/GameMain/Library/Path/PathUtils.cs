using System.IO;
using UnityEngine;

namespace Dragon.Library
{
    //路径处理
    public class PathUtils
    {
        /// <summary>
        /// 标签：是否转移过资源
        /// </summary>
        public const string AssetsTransed_TAG = "AssetsTransed";

        /// <summary>
        /// 标签：是否允许更新，游戏内设置
        /// </summary>
        public const string EnabelUpdate_TAG = "EnabelUpdate";

        /// <summary>
        /// 标签：是否有资源更新，更新流程设置
        /// </summary>
        public const string CanUpdateTo_TAG = "CanUpdateTo";

        //0:编辑器,1:iphone,2:android
        private static int _isBuildType = 0;
        //是否以流的方式读取
        private static bool _isStreaming = false;
        //编辑器状态下,是否使用研发路径
        private static bool _useDevEnvPath = true;

        //这个属性仅对编辑器环境有用,真实环境无用
        public static bool IsDevEnv
        {
            get
            {
                return _useDevEnvPath && Application.isEditor;
            }
            set
            {
                if (_useDevEnvPath != value)
                {
                    _useDevEnvPath = value;
                    ResetProvider();
                }
            }
        }


        private static IPathProvider _provider;

        public static IPathProvider Provider
        {
            get {
                return _provider;
            }
        }

        //构造函数
        static PathUtils()
        {
            Initialize(false);
        }

        //当前是不是通过Stream来读取资源
        public static bool IsStreaming()
        {
            return _isStreaming;
        }

        /// <summary>
        /// 0 pc， 2 android， 1 iphone
        /// </summary>
        /// <returns></returns>
        public static int GetBuildType()
        {
            return _isBuildType;
        }


        private static int DefaultBuildType()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    return 2;
                case RuntimePlatform.IPhonePlayer:
                    return 1;
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.LinuxEditor:
                    return 0;
                case RuntimePlatform.WindowsPlayer:
                    return 3;
                case RuntimePlatform.WebGLPlayer:
                    return 4;
                default:
                    return 0;
            }
        }


        //初始化
        public static void Initialize(bool isStream, int buildType = -1)
        {
            _isStreaming = isStream;
            _isBuildType = ((buildType >= 0) ? buildType : DefaultBuildType());
            ResetProvider();
        }

        private static void ResetProvider()
        {
            _provider = PathProviderFactory.CreateProvider(_isBuildType, _useDevEnvPath);
            _provider.IsStream = _isStreaming;
            Debug.Log("IsStream:" + _isStreaming + ";;BuildType" + _isBuildType + ";;_isMainProject:" + _useDevEnvPath);
            Debug.Log(Infomation());
        }

        //获取App的Streamming路径
        public static string GetAppStreamingPath(string relativePath)
        {
            return _provider.CombinePath(_provider.AppStreamPath, relativePath);
        }

        //获取App的Resource路径
        public static string GetAppResourcePath(string relativePath)
        {
            return _provider.CombinePath(_provider.AppResourceRootPath, relativePath);
        }

        //获取App的Config路径
        public static string GetAppConfigFilePath(string relativePath)
        {
            return _provider.CombinePath(_provider.AppConfigRootPath, relativePath);
        }

        //streaming 路径
        public static string GetStreamingRootPath(string relativePath)
        {
            return _provider.CombinePath(_provider.StreamingRootPath, relativePath);
        }

        //获取资源路径
        public static string GetResourcePath(string relativePath)
        {
            return _provider.CombinePath(_provider.ResourceRootPath, relativePath);
        }

        //获取资源路径
        public static string GetConfigFilePath(string relativePath)
        {
            return _provider.CombinePath(_provider.ConfigRootPath, relativePath);
        }

        //获取配置文件在Resource下面的路径
        public static string GetConfigFilePathRelativeResouce(string fullPath)
        {
            return fullPath.Replace(_provider.ConfigRootPath + "/", "");
        }

        //获取可写路径
        public static string GetWritePath(string relativePath)
        {
            return _provider.CombinePath(_provider.WriteRootPath, relativePath);
        }

        //获取存储区名称
        public static string GetDeviceRootPath(string relativePath)
        {
            return _provider.CombinePath(_provider.DeviceRootPath, relativePath);
        }

        //获取路径中以包名命名的文件夹路径
        public static string GetReleaseResPath(string relativePath)
        {
            return _provider.CombinePath(_provider.ReleaseResRootPath, relativePath);
        }

        //获取资源释放路径的根路径
        public static string GetReleaseResRootPath()
        {
            return _provider.ReleaseResRootPath;
        }

        //配置文件路径，需要查找一下存储路径是否存在，存在则替换路径
        public static string FixedPath(string configFilePath)
        {
            if (configFilePath.IndexOf(_provider.AppStreamPath) >= 0)
            {
                string fixPath = configFilePath.Replace(_provider.AppStreamPath, _provider.ReleaseResRootPath);
                if (System.IO.File.Exists(fixPath))
                {
                    return fixPath;
                }
            }

            return configFilePath;
        }

        public static string Infomation()
        {
            return _provider.Print();
        }
    }
}
