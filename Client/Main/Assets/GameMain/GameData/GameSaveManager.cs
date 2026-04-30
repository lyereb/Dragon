using Newtonsoft.Json;
using System;
using System.Collections;
using UnityEngine;

namespace Dragon.GameData
{
    public enum SaveDataType
    {
        Temp,
        Manual,
        Auto
    }

    public class GameSaveManager
    {
        #region //静态变量

        //静态变量
        protected static GameSaveManager _instance = null;

        //静态属性
        public static GameSaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameSaveManager();
                }
                return _instance;
            }
        }
        #endregion

        /// <summary>
        /// 手动存档槽位数量，槽位索引为 0 .. Max_ManualSaveDataCount-1。
        /// </summary>
        private const int Max_ManualSaveDataCount = 3;
        /// <summary>
        /// 自动存档槽位数量，槽位索引为 0 .. Max_AutoSaveDataCount-1。
        /// </summary>
        private const int Max_AutoSaveDataCount = 3;

        public void Init()
        {

        }

        public void Clear()
        {

        }

        public void SaveMetaData(GameMetaData metaData)
        {
            string dataStream = JsonConvert.SerializeObject(metaData, Formatting.Indented);
            FileIO.WriteData("", "MetaData.json", dataStream);
        }

        public GameMetaData LoadMetaData()
        {
            string dataStream = FileIO.ReadData("", "MetaData.json");
            if (string.IsNullOrEmpty(dataStream))
            {
                Debug.Log("=== GameSaveManager: LoadMetaData failed, dataStream is null ===");
                return null;
            }
            return JsonConvert.DeserializeObject<GameMetaData>(dataStream);
        }


        //开始执行保存之前, 所有需要被保存的数据需要同步到内存，比如有些数据不是一刚改变就实时写入UserData的， 就要在这里统一同步
        public void SyncSaveUserData()
        {

        }

        public void SaveTempUserData(GameMetaData metaData, string uniqueID, GameUserData userData, CustomDataRecord customData = null)
        {
            SyncSaveUserData();
            string dataStream = JsonConvert.SerializeObject(userData, Formatting.Indented);
            SaveDataInfo saveDataInfo = metaData.saveDataInfos[uniqueID];
            if (saveDataInfo.tempSaveData == null || string.IsNullOrEmpty(saveDataInfo.tempSaveData.saveDataName))
                saveDataInfo.tempSaveData = SaveDataRecord.Create(SaveDataType.Temp, $"TempSave_{DateTime.UtcNow.Ticks}.json", customData);
            //更新数据
            saveDataInfo.tempSaveData.saveTimeTicks = DateTime.UtcNow.Ticks;
            saveDataInfo.tempSaveData.customData = customData;
            FileIO.WriteData(uniqueID, saveDataInfo.tempSaveData.saveDataName, dataStream);
        }

        /// <summary>
        /// 当前 uniqueID 在磁盘上是否存在临时存档文件（不读取内容、不触发 ReadData 的缺失日志）
        /// </summary>
        public bool HasTempUserData(GameMetaData metaData, string uniqueID)
        {
            if (metaData == null || !metaData.saveDataInfos.TryGetValue(uniqueID, out SaveDataInfo saveDataInfo))
                return false;
            if (saveDataInfo.tempSaveData == null || string.IsNullOrEmpty(saveDataInfo.tempSaveData.saveDataName))
                return false;
            return FileIO.DataExists(uniqueID, saveDataInfo.tempSaveData.saveDataName);
        }

        /// <param name="manualSaveIndex">手动存档槽位索引，范围 [0, Max_ManualSaveDataCount)。该槽已有文件则直接覆盖。</param>
        public void SaveManualUserData(GameMetaData metaData, string uniqueID, int manualSaveIndex, GameUserData userData, CustomDataRecord customData = null)
        {
            if (manualSaveIndex < 0 || manualSaveIndex >= Max_ManualSaveDataCount)
            {
                Debug.LogWarning($"=== GameSaveManager: SaveManualUserData failed, manualSaveIndex out of range: {manualSaveIndex}, max: {Max_ManualSaveDataCount} ===");
                return;
            }
            if (!metaData.saveDataInfos.TryGetValue(uniqueID, out SaveDataInfo saveDataInfo))
            {
                Debug.LogWarning($"=== GameSaveManager: SaveManualUserData failed, uniqueID:{uniqueID} not found ===");
                return;
            }

            SyncSaveUserData();
            string newSaveDataName = $"ManualSave_{DateTime.UtcNow.Ticks}.json";

            while (saveDataInfo.manualSaveDatas.Count <= manualSaveIndex)
                saveDataInfo.manualSaveDatas.Add(null);

            SaveDataRecord oldRecord = saveDataInfo.manualSaveDatas[manualSaveIndex];
            string oldName = oldRecord?.saveDataName;
            if (!string.IsNullOrEmpty(oldName) && oldName != newSaveDataName)
                FileIO.DeleteData(uniqueID, oldName);

            SaveDataRecord manualRecord = new SaveDataRecord();
            manualRecord.saveDataName = newSaveDataName;
            manualRecord.saveDataType = SaveDataType.Manual;
            manualRecord.saveTimeTicks = DateTime.UtcNow.Ticks;
            manualRecord.customData = customData;
            saveDataInfo.manualSaveDatas[manualSaveIndex] = manualRecord;

            string dataStream = JsonConvert.SerializeObject(userData, Formatting.Indented);
            FileIO.WriteData(uniqueID, newSaveDataName, dataStream);
        }

        public void SaveAutoUserData(GameMetaData metaData, string uniqueID, GameUserData userData, CustomDataRecord customData = null)
        {
            if (!metaData.saveDataInfos.TryGetValue(uniqueID, out SaveDataInfo saveDataInfo))
            {
                Debug.LogWarning($"=== GameSaveManager: SaveAutoUserData failed, uniqueID:{uniqueID} not found ===");
                return;
            }

            SyncSaveUserData();
            string dataStream = JsonConvert.SerializeObject(userData, Formatting.Indented);
            string saveDataName = $"AutoSave_{DateTime.UtcNow.Ticks}.json";
            FileIO.WriteData(uniqueID, saveDataName, dataStream);
            // 自动存档按时间倒序：索引 0 永远是最新，索引越大越旧。
            saveDataInfo.autoSaveDatas.Insert(0, SaveDataRecord.Create(SaveDataType.Auto, saveDataName, customData));

            while (saveDataInfo.autoSaveDatas.Count > Max_AutoSaveDataCount)
            {
                int lastIndex = saveDataInfo.autoSaveDatas.Count - 1;
                string oldestSaveDataName = saveDataInfo.autoSaveDatas[lastIndex]?.saveDataName;
                if (!string.IsNullOrEmpty(oldestSaveDataName))
                    FileIO.DeleteData(uniqueID, oldestSaveDataName);
                saveDataInfo.autoSaveDatas.RemoveAt(lastIndex);
            }
        }

        public GameUserData LoadTempUserData(GameMetaData metaData, string uniqueID)
        {
            SaveDataRecord tempSaveData = metaData.saveDataInfos[uniqueID].tempSaveData;
            if (tempSaveData == null || string.IsNullOrEmpty(tempSaveData.saveDataName))
            {
                Debug.Log("=== GameSaveManager: LoadTempUserData failed, temp save is empty ===");
                return null;
            }
            string dataStream = FileIO.ReadData(uniqueID, tempSaveData.saveDataName);
            if (string.IsNullOrEmpty(dataStream))
            {
                Debug.Log("=== GameSaveManager: LoadTempUserData failed, dataStream is null ===");
                return null;
            }
            return JsonConvert.DeserializeObject<GameUserData>(dataStream);
        }

        public GameUserData LoadManualUserData(GameMetaData metaData, string uniqueID, int index)
        {
            if (!metaData.saveDataInfos.TryGetValue(uniqueID, out SaveDataInfo saveDataInfo))
            {
                Debug.Log("=== GameSaveManager: LoadManualUserData failed, uniqueID not found ===");
                return null;
            }
            if (index < 0 || index >= saveDataInfo.manualSaveDatas.Count)
            {
                Debug.Log("=== GameSaveManager: LoadManualUserData failed, index out of range ===");
                return null;
            }
            string saveDataName = saveDataInfo.manualSaveDatas[index]?.saveDataName;
            if (string.IsNullOrEmpty(saveDataName))
            {
                Debug.Log("=== GameSaveManager: LoadManualUserData failed, slot is empty ===");
                return null;
            }
            string dataStream = FileIO.ReadData(uniqueID, saveDataName);
            if (string.IsNullOrEmpty(dataStream))
            {
                Debug.Log("=== GameSaveManager: LoadManualUserData failed, dataStream is null ===");
                return null;
            }
            return JsonConvert.DeserializeObject<GameUserData>(dataStream);
        }

        public GameUserData LoadAutoUserData(GameMetaData metaData, string uniqueID, int index)
        {
            if (!metaData.saveDataInfos.TryGetValue(uniqueID, out SaveDataInfo saveDataInfo))
            {
                Debug.Log("=== GameSaveManager: LoadAutoUserData failed, uniqueID not found ===");
                return null;
            }
            if (index < 0 || index >= saveDataInfo.autoSaveDatas.Count)
            {
                Debug.Log("=== GameSaveManager: LoadAutoUserData failed, index out of range ===");
                return null;
            }
            string saveDataName = saveDataInfo.autoSaveDatas[index]?.saveDataName;
            if (string.IsNullOrEmpty(saveDataName))
            {
                Debug.Log("=== GameSaveManager: LoadAutoUserData failed, slot is empty ===");
                return null;
            }
            string dataStream = FileIO.ReadData(uniqueID, saveDataName);
            if (string.IsNullOrEmpty(dataStream))
            {
                Debug.Log("=== GameSaveManager: LoadAutoUserData failed, dataStream is null ===");
                return null;
            }
            return JsonConvert.DeserializeObject<GameUserData>(dataStream);
        }

        /// <summary>
        /// 删除指定 uniqueID 在磁盘上的整个存档目录（临时/手动/自动等），并从 metaData 中移除该槽位。
        /// 调用方需在适当时机 <see cref="SaveMetaData"/> 以持久化元数据变更。
        /// </summary>
        public void ClearAllUserDataForUniqueId(GameMetaData metaData, string uniqueID)
        {
            FileIO.DeleteUserDataSubFolder(uniqueID);

            if (metaData?.saveDataInfos != null)
                metaData.saveDataInfos.Remove(uniqueID);

            Debug.Log($"=== GameSaveManager: ClearAllUserDataForUniqueId success, uniqueID:{uniqueID} ===");
        }

        public void ClearTempUserData(GameMetaData metaData, string uniqueID)
        {
            if (!metaData.saveDataInfos.TryGetValue(uniqueID, out SaveDataInfo saveDataInfo))
            {
                Debug.LogWarning($"=== GameSaveManager: ClearTempUserData failed, uniqueID:{uniqueID} not found ===");
                return;
            }

            string tempName = saveDataInfo.tempSaveData?.saveDataName;
            if (!string.IsNullOrEmpty(tempName))
                FileIO.DeleteData(uniqueID, tempName);
            saveDataInfo.tempSaveData = null;
            Debug.Log($"=== GameSaveManager: ClearTempUserData success, uniqueID:{uniqueID}, saveName:{tempName} ===");
            TryRemoveMetaDataIfNoSaveData(metaData, uniqueID, saveDataInfo);
        }

        public void ClearManualUserData(GameMetaData metaData, string uniqueID, int index)
        {
            if (!metaData.saveDataInfos.TryGetValue(uniqueID, out SaveDataInfo saveDataInfo))
            {
                Debug.LogWarning($"=== GameSaveManager: ClearManualUserData failed, uniqueID:{uniqueID} not found ===");
                return;
            }

            if (index < 0 || index >= Max_ManualSaveDataCount)
            {
                Debug.LogWarning($"=== GameSaveManager: ClearManualUserData failed, index out of range, uniqueID:{uniqueID}, index:{index} ===");
                return;
            }

            if (index >= saveDataInfo.manualSaveDatas.Count)
                return;

            string saveDataName = saveDataInfo.manualSaveDatas[index]?.saveDataName;
            if (!string.IsNullOrEmpty(saveDataName))
                FileIO.DeleteData(uniqueID, saveDataName);
            saveDataInfo.manualSaveDatas[index] = null;
            Debug.Log($"=== GameSaveManager: ClearManualUserData success, uniqueID:{uniqueID}, slot:{index} ===");
            TryRemoveMetaDataIfNoSaveData(metaData, uniqueID, saveDataInfo);
        }

        public void ClearAutoUserData(GameMetaData metaData, string uniqueID, int index)
        {
            if (!metaData.saveDataInfos.TryGetValue(uniqueID, out SaveDataInfo saveDataInfo))
            {
                Debug.LogWarning($"=== GameSaveManager: ClearAutoUserData failed, uniqueID:{uniqueID} not found ===");
                return;
            }

            if (index < 0 || index >= saveDataInfo.autoSaveDatas.Count)
            {
                Debug.LogWarning($"=== GameSaveManager: ClearAutoUserData failed, index out of range, uniqueID:{uniqueID}, index:{index} ===");
                return;
            }

            string saveDataName = saveDataInfo.autoSaveDatas[index]?.saveDataName;
            if (!string.IsNullOrEmpty(saveDataName))
                FileIO.DeleteData(uniqueID, saveDataName);
            saveDataInfo.autoSaveDatas.RemoveAt(index);
            Debug.Log($"=== GameSaveManager: ClearAutoUserData success, uniqueID:{uniqueID}, saveName:{saveDataName} ===");
            TryRemoveMetaDataIfNoSaveData(metaData, uniqueID, saveDataInfo);
        }

        private void TryRemoveMetaDataIfNoSaveData(GameMetaData metaData, string uniqueID, SaveDataInfo saveDataInfo)
        {
            if (metaData?.saveDataInfos == null || saveDataInfo == null)
                return;

            string folder = uniqueID;

            bool hasTempData = !string.IsNullOrEmpty(saveDataInfo.tempSaveData?.saveDataName)
                               && FileIO.DataExists(folder, saveDataInfo.tempSaveData.saveDataName);

            bool hasManualData = false;
            for (int i = 0; i < saveDataInfo.manualSaveDatas.Count; i++)
            {
                string fileName = saveDataInfo.manualSaveDatas[i]?.saveDataName;
                if (!string.IsNullOrEmpty(fileName) && FileIO.DataExists(folder, fileName))
                {
                    hasManualData = true;
                    break;
                }
            }

            bool hasAutoData = false;
            for (int i = 0; i < saveDataInfo.autoSaveDatas.Count; i++)
            {
                string fileName = saveDataInfo.autoSaveDatas[i]?.saveDataName;
                if (!string.IsNullOrEmpty(fileName) && FileIO.DataExists(folder, fileName))
                {
                    hasAutoData = true;
                    break;
                }
            }

            if (hasTempData || hasManualData || hasAutoData)
                return;

            metaData.saveDataInfos.Remove(uniqueID);
            FileIO.DeleteUserDataSubFolder(uniqueID);
            Debug.Log($"=== GameSaveManager: remove saveDataInfo because no save files left, uniqueID:{uniqueID} ===");
        }
    }
}