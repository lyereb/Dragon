using System;
using System.Collections.Generic;
using cfg.dragon;
using Dragon.Config;
using Dragon.GameData;
using Dragon.Library;
using UnityEngine;

namespace Dragon.Logic
{
    /// <summary>
    /// 最底层道具入口：校验 item 表、维护用户背包数据、存档与统一事件。
    /// </summary>
    public class ItemManager
    {
        #region //静态变量
        protected static ItemManager _instance = null;

        public static ItemManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ItemManager();
                }
                return _instance;
            }
        }
        #endregion

        private static readonly List<ItemCountDelta> s_deltaBuffer = new List<ItemCountDelta>(8);

        public void Init()
        {
            Debug.Log("ItemManager Init");
        }

        public void Clear()
        {
            Debug.Log("ItemManager Clear");
        }

        /// <summary>道具是否在 item 表中存在。</summary>
        public bool HasItemConfig(int itemId)
        {
            return TryGetItemConfig(itemId, out _);
        }

        public bool TryGetItemConfig(int itemId, out item config)
        {
            config = null;
            var cm = ConfigManager.Instance;
            if (cm == null || !cm.IsInitialized || cm.Tables == null)
            {
                Debug.LogError("ItemManager: 配置未就绪，无法读取 item 表");
                return false;
            }

            config = cm.Tables.Tbitem.GetOrDefault(itemId);
            return config != null;
        }

        /// <summary>当前拥有的数量（未加载用户数据或未入包为 0）。</summary>
        public int GetItemCount(int itemId)
        {
            var bag = GetInventory();
            if (bag?.ItemCounts == null)
            {
                return 0;
            }

            return bag.ItemCounts.TryGetValue(itemId, out int n) ? n : 0;
        }

        /// <summary>
        /// 获得道具（数量须为正；id 须在 item 表中）。
        /// </summary>
        public bool TryGain(int itemId, int count, Action onSuccess = null)
        {
            if (count <= 0)
            {
                return false;
            }

            if (!TryGetItemConfig(itemId, out _))
            {
                Debug.LogWarning($"ItemManager.TryGain: 未配置的道具 id={itemId}");
                return false;
            }

            var bag = GetInventory();
            if (bag?.ItemCounts == null)
            {
                Debug.LogError("ItemManager.TryGain: 用户道具数据不可用");
                return false;
            }

            int oldCount = bag.ItemCounts.TryGetValue(itemId, out int oc) ? oc : 0;
            long sum = (long)oldCount + count;
            if (sum > int.MaxValue)
            {
                Debug.LogError($"ItemManager.TryGain: 数量溢出 itemId={itemId}");
                return false;
            }

            int newCount = (int)sum;
            ApplyCount(bag, itemId, newCount);

            var deltas = SingleDelta(itemId, oldCount, newCount);
            AfterInventoryChanged(deltas, onSuccess);
            return true;
        }

        /// <summary>
        /// 消耗道具；不足则失败且不扣减。
        /// </summary>
        public bool TryConsume(int itemId, int count, Action onSuccess = null)
        {
            if (count <= 0)
            {
                return false;
            }

            if (!TryGetItemConfig(itemId, out _))
            {
                Debug.LogWarning($"ItemManager.TryConsume: 未配置的道具 id={itemId}");
                return false;
            }

            var bag = GetInventory();
            if (bag?.ItemCounts == null)
            {
                Debug.LogError("ItemManager.TryConsume: 用户道具数据不可用");
                return false;
            }

            int oldCount = bag.ItemCounts.TryGetValue(itemId, out int oc) ? oc : 0;
            if (oldCount < count)
            {
                return false;
            }

            int newCount = oldCount - count;
            ApplyCount(bag, itemId, newCount);

            var deltas = SingleDelta(itemId, oldCount, newCount);
            AfterInventoryChanged(deltas, onSuccess);
            return true;
        }

        /// <summary>
        /// 批量消耗：任一不满足则全部不扣减（原子性）。同一 itemId 出现多次会先合并需求量。
        /// </summary>
        public bool TryConsumeBatch(IReadOnlyList<(int itemId, int count)> costs, Action onSuccess = null)
        {
            if (costs == null || costs.Count == 0)
            {
                return false;
            }

            var bag = GetInventory();
            if (bag?.ItemCounts == null)
            {
                Debug.LogError("ItemManager.TryConsumeBatch: 用户道具数据不可用");
                return false;
            }

            var mergedNeed = new Dictionary<int, int>(costs.Count);
            for (int i = 0; i < costs.Count; i++)
            {
                var c = costs[i];
                if (c.count <= 0)
                {
                    return false;
                }

                if (!TryGetItemConfig(c.itemId, out _))
                {
                    Debug.LogWarning($"ItemManager.TryConsumeBatch: 未配置的道具 id={c.itemId}");
                    return false;
                }

                mergedNeed[c.itemId] = mergedNeed.TryGetValue(c.itemId, out int sum) ? sum + c.count : c.count;
            }

            s_deltaBuffer.Clear();
            foreach (var kv in mergedNeed)
            {
                int itemId = kv.Key;
                int need = kv.Value;
                int oldCount = bag.ItemCounts.TryGetValue(itemId, out int oc) ? oc : 0;
                if (oldCount < need)
                {
                    s_deltaBuffer.Clear();
                    return false;
                }

                s_deltaBuffer.Add(new ItemCountDelta(itemId, oldCount, oldCount - need));
            }

            for (int i = 0; i < s_deltaBuffer.Count; i++)
            {
                var d = s_deltaBuffer[i];
                ApplyCount(bag, d.ItemId, d.NewCount);
            }

            var snapshot = s_deltaBuffer.ToArray();
            s_deltaBuffer.Clear();
            AfterInventoryChanged(snapshot, onSuccess);
            return true;
        }

        private static UserItemInventoryData GetInventory()
        {
            return GameDataManager.Instance?.UserData?.ItemInventory;
        }

        private static void ApplyCount(UserItemInventoryData bag, int itemId, int newCount)
        {
            if (newCount <= 0)
            {
                bag.ItemCounts.Remove(itemId);
            }
            else
            {
                bag.ItemCounts[itemId] = newCount;
            }
        }

        private static ItemCountDelta[] SingleDelta(int itemId, int oldCount, int newCount)
        {
            return new[] { new ItemCountDelta(itemId, oldCount, newCount) };
        }

        private static void AfterInventoryChanged(IReadOnlyList<ItemCountDelta> deltas, Action onSuccess)
        {
            PersistUserDataIfPossible();
            var param = new ItemChangeEventParam(deltas);
            EventManager.Instance.TriggerEvent(MainEventDefine.EID_ITEM_INVENTORY_CHANGED, param, null, null, true, 0f);
            onSuccess?.Invoke();
        }

        private static void PersistUserDataIfPossible()
        {
            var dm = GameDataManager.Instance;
            if (dm?.UserData == null || dm.MetaData == null)
            {
                return;
            }

            string id = dm.MetaData.currentUniqueID;
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            dm.SaveTempUserData(id);
        }
    }
}
