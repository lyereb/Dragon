namespace Dragon.Logic
{
    /// <summary>
    /// Provider 模板：复制此类到 Providers/ 并实现 Evaluate。
    /// </summary>
    public sealed class RedDotExampleProvider : IRedDotProvider
    {
        private readonly string _key;
        private readonly RedDotRefreshMode _refreshMode;
        private readonly int[] _watchEventIds;

        public RedDotExampleProvider(
            string key,
            RedDotRefreshMode refreshMode = RedDotRefreshMode.Dynamic,
            params int[] watchEventIds)
        {
            _key = key;
            _refreshMode = refreshMode;
            _watchEventIds = watchEventIds ?? System.Array.Empty<int>();
        }

        public string Key => _key;

        public RedDotRefreshMode RefreshMode => _refreshMode;

        public int[] WatchEventIds => _watchEventIds;

        public RedDotValue Evaluate()
        {
            // TODO: 读取业务 Manager 数据并返回 Count
            return RedDotValue.None;
        }
    }
}
