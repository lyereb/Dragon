using System;
using UnityEngine;

namespace Dragon.Library
{
    public class PathProviderFactory
    {
        public static IPathProvider CreateProvider(int type,bool isMainProject)
        {
            switch (type)
            {
                case 0:
                    if (isMainProject)
                    {
                        return new PathProviderMainEditor();
                    }
                    else
                    {
                        return new PathProviderEditor();
                    }
                case 1:
                    return new PathProviderIOS();
                case 2:
                    return new PathProviderAndroid();
                case 3:
                    return new PathProviderWin();
                case 4:
                    return new PathProviderWebGL();
                default:
                    Debug.LogError("BuildType Set Fail!! " + type);
                    return null;
            }
        }
    }
}
