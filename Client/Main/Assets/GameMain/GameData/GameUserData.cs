using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Dragon.GameData
{
    public class GameUserData
    {
        public UserPlayerData PlayerData;
        public UserItemInventoryData ItemInventory;

        public void NewData()
        {
            PlayerData = new UserPlayerData();
            PlayerData.NewData();
            ItemInventory = new UserItemInventoryData();
            ItemInventory.NewData();
        }

        public bool EnsureLoadedData()
        {
            bool hasChanged = false;
            FieldInfo[] fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                Type fieldType = field.FieldType;
                if (fieldType.IsValueType || fieldType == typeof(string))
                {
                    continue;
                }

                object value = field.GetValue(this);
                if (value != null)
                {
                    continue;
                }

                object newValue = Activator.CreateInstance(fieldType);
                MethodInfo newDataMethod = fieldType.GetMethod("NewData", BindingFlags.Public | BindingFlags.Instance);
                newDataMethod?.Invoke(newValue, null);
                field.SetValue(this, newValue);
                hasChanged = true;
            }

            return hasChanged;
        }
    }

    #region DataDefine
    public class GameMetaData
    {
        public string currentUniqueID;
        public Dictionary<string, SaveDataInfo> saveDataInfos;

        public void NewData()
        {
            currentUniqueID = string.Empty;
            saveDataInfos = new Dictionary<string, SaveDataInfo>();
        }
    }

    public class SaveDataInfo
    {
        public string uniqueID;
        //临时存档 用于切换场景等等
        public SaveDataRecord tempSaveData;
        //手动存档
        public List<SaveDataRecord> manualSaveDatas;
        //自动存档
        public List<SaveDataRecord> autoSaveDatas;

        public void NewData(string uniqueID)
        {
            this.uniqueID = uniqueID;
            tempSaveData = SaveDataRecord.Create(SaveDataType.Temp, $"TempSave_{DateTime.UtcNow.Ticks}.json");
            manualSaveDatas = new List<SaveDataRecord>();
            autoSaveDatas = new List<SaveDataRecord>();
        }
    }

    public class SaveDataRecord
    {
        public string saveDataName;
        public SaveDataType saveDataType;
        public long saveTimeTicks;
        public CustomDataRecord customData;

        public static SaveDataRecord Create(SaveDataType saveDataType, string saveDataName, CustomDataRecord customData = null)
        {
            return new SaveDataRecord
            {
                saveDataName = saveDataName,
                saveDataType = saveDataType,
                saveTimeTicks = DateTime.UtcNow.Ticks,
                customData = customData
            };
        }
    }

    /// <summary>
    /// 存档元数据附加信息（如读档 UI 展示关卡名）。
    /// </summary>
    public class CustomDataRecord
    {
        /// <summary>读档列表等 UI 用摘要文案（如关卡名）。</summary>
        public string summary;
    }

    public class UserPlayerData
    {
        public int Level;

        public void NewData()
        {
            Level = 1;
        }
    }

    /// <summary>
    /// 用户侧道具背包（存档字段，由 ItemManager 读写）。
    /// </summary>
    public class UserItemInventoryData
    {
        /// <summary>道具 id -> 持有数量</summary>
        public Dictionary<int, int> ItemCounts;

        public void NewData()
        {
            ItemCounts = new Dictionary<int, int>();
        }
    }

    /// <summary>
    /// 仅用于 JSON 存档的向量，避免直接序列化 UnityEngine.Vector3（其属性如 normalized 会形成自引用循环）。
    /// </summary>
    [Serializable]
    public struct SaveVector3
    {
        public float x;
        public float y;
        public float z;

        public SaveVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static implicit operator Vector3(SaveVector3 v) => new Vector3(v.x, v.y, v.z);
        public static implicit operator SaveVector3(Vector3 v) => new SaveVector3(v.x, v.y, v.z);
    }
    #endregion
}