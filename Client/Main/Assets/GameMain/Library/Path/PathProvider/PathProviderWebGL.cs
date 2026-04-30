using System.IO;
using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// Android下的路径设定 
    /// android的路径大部分是有app的持续化路径组成.
    /// </summary>
    public class PathProviderWebGL : PathProviderBase
    {
        protected override string OnGetAppFilePath()
        {
            return AppDataPath;
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
            return AppStreamPath;
        }
        protected override string OnGetReleaseResRootPath()
        {
            return OnGetStreamingRootPath();
        }

        protected override string OnGetWriteRootPath()
        {
            return AppStreamPath + "/TDFiles";
        }

        protected override string OnGetDeviceRootPath()
        {
            return AppStreamPath;
          
        }

        

        protected override string OnGetConfigRootPath()
        {
            return AppStreamPath + "/gameassets/runassets/config";
        }

        protected override string OnGetResourceRootPath()
        {
            if (IsStream)
            {
                return AppStreamPath + "/gameassets/runassets";
            }
            else
            {
                return "";
            }
        }
    }
}
