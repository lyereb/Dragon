using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// 编辑器状态下的路径设定
    /// 编辑状态子啊的路径基本上在appdata路径组成
    /// </summary>
    public class PathProviderMainEditor : PathProviderBase
    {
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
            return AppDataPath + "/StreamingAssets";
        }
        protected override string OnGetReleaseResRootPath()
        {
            return OnGetStreamingRootPath();
        }

        protected override string OnGetWriteRootPath()
        {
            return AppPersistentPath+ "/TDFiles";
        }

        protected override string OnGetConfigRootPath()
        {
            if (IsStream)
            {
                return AppDataPath + "/StreamingAssets/gameassets/runassets/config";
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
                return AppDataPath + "/StreamingAssets/gameassets/runassets";
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
    }
}
