namespace Dragon.Logic
{
    /// <summary>
    /// 叶子红点的刷新/消隐策略。
    /// </summary>
    public enum RedDotRefreshMode
    {
        /// <summary>数据变化即重算（未读数、可领取等）。</summary>
        Dynamic = 0,

        /// <summary>进入对应界面或调用 MarkSeen 后消隐，持久化已读。</summary>
        SeenUntilEnter = 1,

        /// <summary>本次进程内有效，MarkSeen 后消隐，不持久化。</summary>
        OncePerSession = 2,
    }
}
