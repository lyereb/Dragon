using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// IOS的路径设定
    /// 基本上也是由appdata组成
    /// </summary>
    public class PathProviderIOS : PathProviderBase
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
            return AppPersistentPath + "/StreamingAssets";
        }

        protected override string OnGetWriteRootPath()
        {
            return AppPersistentPath+ "/TDFiles";
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
                return "Assets/gameassets/runassets";
            }
        }

        protected override string OnGetDeviceRootPath()
        {
            return AppPersistentPath;
        }

        protected override string OnGetReleaseResRootPath()
        {
            return OnGetStreamingRootPath();
        }
    }
}
