namespace Dragon.Logic
{
    /// <summary>
    /// 叶子红点计算：只读业务数据，不操作 UI。
    /// 实现类放在 <c>Logic/RedDot/Providers/</c>。
    /// </summary>
    public interface IRedDotProvider
    {
        string Key { get; }

        RedDotRefreshMode RefreshMode { get; }

        /// <summary>返回当前应展示的红点数值。</summary>
        RedDotValue Evaluate();

        /// <summary>
        /// 监听的事件 ID；变化时 Manager 会对该 key MarkDirty。
        /// 无事件驱动时可返回空数组，由业务侧手动 MarkDirty。
        /// </summary>
        int[] WatchEventIds { get; }
    }
}
