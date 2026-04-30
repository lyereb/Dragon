using System;
using System.Collections;
using UnityEngine;

namespace Dragon.GameData
{
    public class GameDataManager
    {
        #region //静态变量

        //静态变量
        protected static GameDataManager _instance = null;

        //静态属性
        public static GameDataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameDataManager();
                }
                return _instance;
            }
        }
        #endregion

        private GameMetaData metaData;
        public GameMetaData MetaData => metaData;

        private GameUserData userData;
        public GameUserData UserData => userData;

        public void Init()
        {
            LoadMetaData();
        }

        public void Clear()
        {

        }

        /// <summary>
        /// 创建新的存档信息管理文件
        /// </summary>
        private void CreateMetaData()
        {
            metaData = new GameMetaData();
            metaData.NewData();
            SaveMetaData();
        }

        /// <summary>
        /// 创建新的存档组信息, 用于不同角色内部有多个存档的情况
        /// </summary>
        /// <param name="uniqueID"></param>
        public void CreateNewSaveDataInfo(string uniqueID)
        {
            SaveDataInfo saveDataInfo = new SaveDataInfo();
            saveDataInfo.NewData(uniqueID);
            metaData.saveDataInfos.Add(saveDataInfo.uniqueID, saveDataInfo);
            SaveMetaData();
        }

        private void SaveMetaData()
        {
            GameSaveManager.Instance.SaveMetaData(metaData);
        }

        private void LoadMetaData()
        {
            metaData = GameSaveManager.Instance.LoadMetaData();
            if (metaData == null)
            {
                CreateMetaData();
            }
        }

        /// <summary>
        /// 创建新的临时存档,一个角色一定需要一个临时存档
        /// </summary>
        /// <param name="uniqueID"></param>
        public void CreateNewTempUserData(string uniqueID)
        {
            userData = new GameUserData();
            userData.NewData();
            SaveTempUserData(uniqueID);
        }

        /// <summary>
        /// 保存临时存档数据
        /// </summary>
        /// <param name="uniqueID"></param>
        public void SaveTempUserData(string uniqueID)
        {
            if (!metaData.saveDataInfos.ContainsKey(uniqueID))
            {
                //如果保存的时候metaData没有这个uniqueID，则创建一个新的
                CreateNewSaveDataInfo(uniqueID);
                Debug.LogWarning("=== GameDataManager: 原存档meta已经被删除，创建新的存档meta ===");
            }
            GameSaveManager.Instance.SaveTempUserData(metaData, uniqueID, userData);
            SaveMetaData();
        }

        /// <summary>
        /// 保存手动存档数据
        /// </summary>
        /// <param name="uniqueID"></param>
        /// <param name="manualSaveIndex"></param>
        public void SaveManualUserData(string uniqueID, int manualSaveIndex)
        {
            if (!metaData.saveDataInfos.ContainsKey(uniqueID))
            {
                //如果保存的时候metaData没有这个uniqueID，则创建一个新的
                CreateNewSaveDataInfo(uniqueID);
                SaveTempUserData(uniqueID);
                Debug.LogWarning("=== GameDataManager: 原存档meta已经被删除，创建新的存档meta ===");
            }
            GameSaveManager.Instance.SaveManualUserData(metaData, uniqueID, manualSaveIndex, userData);
            SaveMetaData();
        }

        /// <summary>
        /// 保存自动存档数据
        /// </summary>
        /// <param name="uniqueID"></param>
        public void SaveAutoUserData(string uniqueID)
        {
            if (!metaData.saveDataInfos.ContainsKey(uniqueID))
            {
                //如果保存的时候metaData没有这个uniqueID，则创建一个新的
                CreateNewSaveDataInfo(uniqueID);
                SaveTempUserData(uniqueID);
                Debug.LogWarning("=== GameDataManager: 原存档meta已经被删除，创建新的存档meta ===");
            }
            GameSaveManager.Instance.SaveAutoUserData(metaData, uniqueID, userData);
            SaveMetaData();
        }

        public void LoadTempUserData(string uniqueID)
        {
            userData = GameSaveManager.Instance.LoadTempUserData(metaData, uniqueID);
            if (userData == null)
            {
                Debug.LogError("=== GameDataManager: LoadTempUserData failed, userData is null, create new auto data ===");
                return;
            }

            bool hasPatchedData = userData.EnsureLoadedData();
            if (hasPatchedData)
            {
                Debug.Log("=== GameDataManager: Loaded user data patched with newly added sub data ===");
            }

            metaData.currentUniqueID = uniqueID;
            SaveMetaData();
        }

        public void LoadManualUserData(string uniqueID, int index)
        {
            userData = GameSaveManager.Instance.LoadManualUserData(metaData, uniqueID, index);
            if (userData == null)
            {
                Debug.Log("=== GameDataManager: LoadUserData failed,index out of range ===");
                return;
            }

            bool hasPatchedData = userData.EnsureLoadedData();
            if (hasPatchedData)
            {
                Debug.Log("=== GameDataManager: Loaded user data patched with newly added sub data ===");
            }

            metaData.currentUniqueID = uniqueID;
            SaveMetaData();
        }

        public void LoadAutoUserData(string uniqueID, int index)
        {
            userData = GameSaveManager.Instance.LoadAutoUserData(metaData, uniqueID, index);
            if (userData == null)
            {
                Debug.Log("=== GameDataManager: LoadAutoUserData failed,index out of range ===");
                return;
            }

            bool hasPatchedData = userData.EnsureLoadedData();
            if (hasPatchedData)
            {
                Debug.Log("=== GameDataManager: Loaded user data patched with newly added sub data ===");
            }

            metaData.currentUniqueID = uniqueID;
            SaveMetaData();
        }

        public void ClearAllUserDataForUniqueId(string uniqueID)
        {
            GameSaveManager.Instance.ClearAllUserDataForUniqueId(metaData, uniqueID);
            SaveMetaData();
        }

        public void ClearTempUserData(string uniqueID)
        {
            GameSaveManager.Instance.ClearTempUserData(metaData, uniqueID);
            SaveMetaData();
        }

        public void ClearManualUserData(string uniqueID, int index)
        {
            GameSaveManager.Instance.ClearManualUserData(metaData, uniqueID, index);
            SaveMetaData();
        }

        public void ClearAutoUserData(string uniqueID, int index)
        {
            GameSaveManager.Instance.ClearAutoUserData(metaData, uniqueID, index);
            SaveMetaData();
        }
    }
}