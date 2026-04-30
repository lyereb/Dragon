using System;
using System.Text;
using UnityEngine;

namespace Dragon.Library
{
    //路径提供者的基础
    public abstract class PathProviderBase : IPathProvider
    {

        //本地版本文件
        public const string CN_LOCALVERSION_NAME = "localversion.json";
        //是否是流数据
        private bool _isStream = false;
        //设备存储的根目录--用于存储一些即使包被卸载了，也能保存的数据
        private string _deviceRootPath = null;
        //app文件路径
        private string _appFilePath = null;
        //app中的Resouce目录
        private string _appResourceRootPath = null;
        //app中的配置目录
        private string _appConfigRootPath = null;
        //资源流所在目录
        private string _streamingRootPath = null;
        //需要写入的目录:比如日志,截图
        private string _writeRootPath = null;
        //资源所在目录
        private string _resourceRootPath = null;
        //配置文件所在目录
        private string _configRootPath = null;        
        //包释放资源所在目录
        private string _releaseResRootPath = null;



        //构造函数
        public PathProviderBase()
        {
            AppDataPath = Application.dataPath;
            AppStreamPath = Application.streamingAssetsPath;
            AppPersistentPath = Application.persistentDataPath;
        }

        //是否通过Bundle来进行加载资源
        public bool IsStream
        {
            get
            {
                return _isStream;
            }

            set
            {
                if (_isStream != value)
                {
                    _isStream = value;
                    //如果IsStream的加载方式改变,那么就把所有路径字符串清空
                    _streamingRootPath = null;
                    _writeRootPath = null;
                    _resourceRootPath = null;
                    _configRootPath = null;
                    _deviceRootPath = null;
                    _releaseResRootPath = null;
                }

            }
        }


        //app的数据路径
        public string AppDataPath { get; protected set; }
        //app的stream路径
        public string AppStreamPath { get; private set; }
        //app的持续化路径
        public string AppPersistentPath { get; private set; }

        public string AppFilePath {
            get {

                if (_appFilePath == null)
                {
                    _appFilePath = OnGetAppFilePath();
                }
                return _appFilePath;
            }
        }

        public string AppResourceRootPath
        {
            get
            {
                if (_appResourceRootPath == null)
                {
                    _appResourceRootPath = OnGetAppResourceRootPath();
                }
                return _appResourceRootPath;

            }
        }


        public string AppConfigRootPath
        {
            get
            {
                if (_appConfigRootPath == null)
                {
                    _appConfigRootPath = OnGetAppConfigRootPath();
                }
                return _appConfigRootPath;
            }
        }

        //资源的Stream根路径
        public string StreamingRootPath
        {
            get
            {
                if (_streamingRootPath == null)
                    _streamingRootPath = OnGetStreamingRootPath();
                return _streamingRootPath;
            }
        }
        //可写的根路径
        public string WriteRootPath
        {
            get
            {
                if (_writeRootPath == null)
                    _writeRootPath = OnGetWriteRootPath();
                return _writeRootPath;
            }
        }
        //配置文件的路径
        public string ConfigRootPath
        {
            get
            {
                if (_configRootPath == null)
                    _configRootPath = OnGetConfigRootPath();
                return _configRootPath;
            }
        }
        //资源的根路径
        public string ResourceRootPath
        {
            get
            {
                if (_resourceRootPath == null)
                    _resourceRootPath = OnGetResourceRootPath();
                return _resourceRootPath;
            }
        }
        //apk包的路径
        public string DeviceRootPath
        {
            get
            {
                if (_deviceRootPath == null)
                    _deviceRootPath = OnGetDeviceRootPath();
                return _deviceRootPath;
            }
        }
        //释放资源的路径
        public string ReleaseResRootPath
        {
            get
            {
                if (_releaseResRootPath == null)
                    _releaseResRootPath = OnGetReleaseResRootPath();
                return _releaseResRootPath;
            }
        }

        public string UnityCachePath
        {
            get
            {
                return OnGetUnityCachePath();
            }
        }

        //是否需要释放
        public bool IsNeedRelease
        {
            get
            {
                return OnCheckRelease();
            }
        }

        //打印路径
        public string Print()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Print All PathList:");
            sb.AppendFormat("AppDataPath:{0}", AppDataPath);sb.AppendLine();
            sb.AppendFormat("AppStreamPath:{0}", AppStreamPath); sb.AppendLine();
            sb.AppendFormat("AppPersistentPath:{0}", AppPersistentPath); sb.AppendLine();
            sb.AppendFormat("StreamingRootPath:{0}", StreamingRootPath); sb.AppendLine();
            sb.AppendFormat("WriteRootPath:{0}", WriteRootPath); sb.AppendLine();
            sb.AppendFormat("ConfigRootPath:{0}", ConfigRootPath); sb.AppendLine();
            sb.AppendFormat("ResourceRootPath:{0}", ResourceRootPath); sb.AppendLine();
            sb.AppendFormat("DeviceRootPath:{0}", DeviceRootPath); sb.AppendLine();
            sb.AppendFormat("ReleaseResRootPath:{0}", ReleaseResRootPath); sb.AppendLine();
            return sb.ToString();
        }

        //组合路径
        public string CombinePath(string rootPath, string relativePath)
        {
            return OnCombinePath(rootPath, relativePath);
        }


        #region//抽象函数,保护类型,由子类进行重写

        protected abstract string OnGetConfigRootPath();

        protected abstract string OnGetDeviceRootPath();

        protected abstract string OnGetReleaseResRootPath();

        protected abstract string OnGetResourceRootPath();

        protected abstract string OnGetStreamingRootPath();

        protected abstract string OnGetWriteRootPath();

        protected abstract string OnGetAppFilePath();
        protected abstract string OnGetAppResourceRootPath();
        protected abstract string OnGetAppConfigRootPath();

        protected virtual string OnGetUnityCachePath()
        {
            return Caching.currentCacheForWriting.path;
        }

        protected virtual bool OnCheckRelease()
        {
            return String.Equals(ReleaseResRootPath, AppStreamPath, StringComparison.CurrentCultureIgnoreCase);
        }

        //组合路径
        protected virtual string OnCombinePath(string rootPath, string relativePath)
        {
            //如果RootPath是一个空目录,那么就直接返回
            if (string.IsNullOrEmpty(rootPath)) return relativePath;

            //如果relativePath路径中的根目录是rootPath,表示relationPath已经是一个绝对路径了,就直接返回.
            if (relativePath.StartsWith(rootPath)) return relativePath;

            if (relativePath != string.Empty && relativePath[0] != '/')
            {
                relativePath = "/" + relativePath;
            }
            return rootPath + relativePath;
        }

        #endregion


    }
}
