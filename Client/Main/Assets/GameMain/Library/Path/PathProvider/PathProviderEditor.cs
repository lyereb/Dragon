using System;
using System.IO;
using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// 编辑器状态下的路径设定
    /// 编辑状态子啊的路径基本上在appdata路径组成
    /// </summary>
    public class PathProviderEditor : PathProviderBase
    {
        private string _persistentPath = null;
        private string PersistentPath {
            get
            {
                if (string.IsNullOrEmpty(_persistentPath))
                {
                    System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(AppDataPath);
                    _persistentPath = di.Parent.FullName + "/Persistent";
                }
                return _persistentPath;
            }
        }

        protected override string OnGetAppFilePath()
        {
            return AppStreamPath + "/gameassets/runassets/config/localversion.json";
        }


        protected override string OnGetAppConfigRootPath()
        {
            return AppStreamPath + "/gameassets/runassets/config";
        }

        protected override string OnGetAppResourceRootPath()
        {
            return AppStreamPath + "/gameassets/runassets";
        }

        protected override string OnGetStreamingRootPath()
        {
            return PersistentPath + "/StreamingAssets";
        }

        protected override string OnGetReleaseResRootPath()
        {
            return OnGetStreamingRootPath();
        }

        protected override string OnGetWriteRootPath()
        {
            return PersistentPath + "/TDFiles";
        }

        protected override string OnGetConfigRootPath()
        {
            if (IsStream)
            {
                return PersistentPath + "/StreamingAssets/gameassets/runassets/config";
            }
            else
            {
                return "Assets/gameassets/runassets/config";
            }
        }

        protected override string OnGetResourceRootPath()
        {
            if (IsStream)
            {
                return PersistentPath + "/StreamingAssets/gameassets/runassets";
            }
            else
            {
                return "Assets/gameassets/runassets";
            }
        }

        protected override string OnGetDeviceRootPath()
        {
            return "C:\\";
        }

        protected override bool OnCheckRelease()
        {
            return false;
        }
    }
}
