namespace Dragon.Logic
{
    /// <summary>
    /// 非叶子节点对子节点的聚合方式。
    /// </summary>
    public enum RedDotAggregateMode
    {
        /// <summary>任一子节点 Visible 则父节点 Count=1。</summary>
        Or = 0,

        /// <summary>父节点 Count = 所有子节点 Count 之和。</summary>
        Sum = 1,

        /// <summary>父节点 Count = 子节点 Count 最大值。</summary>
        Max = 2,
    }
}
