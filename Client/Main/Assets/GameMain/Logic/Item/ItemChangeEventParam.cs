using System.Collections.Generic;

namespace Dragon.Logic
{
    /// <summary>
    /// 单次道具数量变化描述（用于事件与调试）。
    /// </summary>
    public readonly struct ItemCountDelta
    {
        public int ItemId { get; }
        public int OldCount { get; }
        public int NewCount { get; }

        public ItemCountDelta(int itemId, int oldCount, int newCount)
        {
            ItemId = itemId;
            OldCount = oldCount;
            NewCount = newCount;
        }
    }

    /// <summary>
    /// 道具变更事件的统一参数（一次 API 调用内可能有多条 delta）。
    /// </summary>
    public sealed class ItemChangeEventParam
    {
        public IReadOnlyList<ItemCountDelta> Deltas { get; }

        public ItemChangeEventParam(IReadOnlyList<ItemCountDelta> deltas)
        {
            Deltas = deltas;
        }
    }
}
