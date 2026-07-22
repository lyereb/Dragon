namespace Dragon.Logic
{
    /// <summary>
    /// 红点数值：Count &gt; 0 即视为可见。
    /// </summary>
    public readonly struct RedDotValue
    {
        public static readonly RedDotValue None = new RedDotValue(0);

        public int Count { get; }

        public bool Visible => Count > 0;

        public RedDotValue(int count)
        {
            Count = count < 0 ? 0 : count;
        }
    }
}
