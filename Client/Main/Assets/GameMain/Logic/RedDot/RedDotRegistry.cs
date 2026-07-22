namespace Dragon.Logic
{
    /// <summary>
    /// 红点树与 Provider 注册入口。接入 GameCenter 前在此集中配置。
    /// </summary>
    public static class RedDotRegistry
    {
        public static void RegisterAll(RedDotManager manager)
        {
            // --- 非叶子：只做聚合 ---
            manager.RegisterNode(RedDotKey.Lobby, parentKey: null, RedDotAggregateMode.Or);
            manager.RegisterNode(RedDotKey.LobbyMail, RedDotKey.Lobby, RedDotAggregateMode.Sum);

            // --- 叶子：绑定 Provider（示例，默认未启用）---
            // manager.RegisterLeaf(RedDotKey.LobbyMailSystem, RedDotKey.LobbyMail, new RedDotExampleProvider(RedDotKey.LobbyMailSystem));
            // manager.RegisterLeaf(RedDotKey.FightChat, parentKey: null, new RedDotChatProvider());
        }
    }
}
