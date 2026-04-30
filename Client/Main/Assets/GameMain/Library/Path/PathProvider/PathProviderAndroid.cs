using System.IO;
using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// Android下的路径设定 
    /// android的路径大部分是有app的持续化路径组成.
    /// </summary>
    public class PathProviderAndroid : PathProviderBase
    {
        public PathProviderAndroid():base()
        {
            //应对aab文件的处理
            var tmp = System.IO.Path.GetDirectoryName(AppDataPath);
            tmp = tmp + "/split_base_assets.apk";
            if (File.Exists(tmp))
            {
                AppDataPath = tmp;
            }
        }

        protected override string OnGetAppFilePath()
        {
            return AppDataPath;
        }

        protected override string OnGetAppConfigRootPath()
        {
            return "/gameassets/runassets/config".ToLower();
        }

        protected override string OnGetAppResourceRootPath()
        {
            return "/gameassets/runassets";
        }

        protected override string OnGetStreamingRootPath()
        {
            return AppPersistentPath + "/StreamingAssets";
        }
        protected override string OnGetReleaseResRootPath()
        {
            return OnGetStreamingRootPath();
        }

        protected override string OnGetWriteRootPath()
        {
            return AppPersistentPath + "/TDFiles";
        }

        protected override string OnGetDeviceRootPath()
        {
            return AppPersistentPath;
            /* android 11之后 禁止调用外部存储了.
            if (AppPersistentPath.Contains("Android"))
            {
                return AppPersistentPath.Substring(0, AppPersistentPath.IndexOf("Android"));
            }
            else
            {
                return AppPersistentPath + "/..";
            } */
        }

        

        protected override string OnGetConfigRootPath()
        {
            return AppPersistentPath + "/StreamingAssets/gameassets/runassets/config";
        }

        protected override string OnGetResourceRootPath()
        {
            if (IsStream)
            {
                return AppPersistentPath + "/StreamingAssets/gameassets/runassets";
            }
            else
            {
                return "";
            }
        }

        protected override string OnGetUnityCachePath()
        {
            return AppPersistentPath + "/UnityCache/Shared";
        }
    }
}
