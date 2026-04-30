
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class FileIO
{
    static string userdataPath = GetPersistentDataPath("UserData");
    static string GetPersistentDataPath(string path)
    {
#if UNITY_EDITOR             
        return Application.dataPath + "/AddressableAssets/" + path;
#else
        return Application.persistentDataPath + "/" + path;
#endif
    }

    #region 读取模块
    /// <summary>
    /// 读取用户游戏数据
    /// </summary>
    public static string ReadData(string subFolder, string dataName)
    {
        string dataPath = Path.Combine(userdataPath, subFolder, dataName);
        return Read(dataPath);
    }

    /// <summary>
    /// 判断指定用户数据文件是否存在（与 ReadData 使用相同路径规则）
    /// </summary>
    public static bool DataExists(string subFolder, string dataName)
    {
        string dataPath = Path.Combine(userdataPath, subFolder, dataName);
        return File.Exists(dataPath);
    }

    static string Read(string dataPath)
    {
        if (File.Exists(dataPath))
        {
            string stream = File.ReadAllText(dataPath, System.Text.Encoding.UTF8);
            return stream;
        }
        Debug.Log("=== ReadWrite: not file found in path: " + dataPath + "===");
        return null;
    }


    #endregion

    #region 写入模块
    /// <summary>
    /// 用户存档数据
    /// </summary>

    public static void WriteData(string subFolder, string dataName, string dataStream)
    {
        CreateFolder(userdataPath);
        CreateFolder(Path.Combine(userdataPath, subFolder));
        string dataPath = Path.Combine(userdataPath, subFolder, dataName);
        Write(dataPath, dataStream);
    }

    //创建用户数据文件夹
    static void CreateFolder(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    static void Write(string dataPath, string stream)
    {
        File.WriteAllText(dataPath, stream, System.Text.Encoding.UTF8);
    }

    #endregion

    #region 删除玩家所有数据
    public static void DeleteData(string subFolder, string dataName)
    {
        string dataPath = Path.Combine(userdataPath, subFolder, dataName);
        if (!File.Exists(dataPath))
        {
            Debug.LogWarning("=== ReadWrite: file does not exist. path: " + dataPath + " ===");
            return;
        }

        File.Delete(dataPath);
    }

    /// <summary>
    /// 删除指定子目录下的全部用户数据文件（与 WriteData 使用相同 userdata 根路径；subFolder 为空时拒绝，避免误删根目录）
    /// </summary>
    public static void DeleteUserDataSubFolder(string subFolder)
    {
        if (string.IsNullOrEmpty(subFolder))
        {
            Debug.LogWarning("=== ReadWrite: DeleteUserDataSubFolder refused empty subFolder ===");
            return;
        }

        string dir = Path.Combine(userdataPath, subFolder);
        if (!Directory.Exists(dir))
            return;

        Directory.Delete(dir, true);

#if UNITY_EDITOR
        // In editor mode the save path is under Assets. Remove the folder's .meta as well,
        // otherwise Unity may still keep a stale asset entry for this path.
        string dirMeta = dir + ".meta";
        if (File.Exists(dirMeta))
            File.Delete(dirMeta);
        AssetDatabase.Refresh();
#endif
    }
    #endregion
}
