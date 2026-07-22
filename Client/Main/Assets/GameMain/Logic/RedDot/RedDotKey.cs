namespace Dragon.Logic
{
    /// <summary>
    /// 红点 key 常量。非叶子只做聚合；叶子绑定 <see cref="IRedDotProvider"/>。
    /// 约定：用 '.' 分段表示层级，如 lobby.mail.system。
    /// </summary>
    public static class RedDotKey
    {
        #region 示例（接入业务时按需增删）

        public const string Lobby = "lobby";
        public const string LobbyMail = "lobby.mail";
        public const string LobbyMailSystem = "lobby.mail.system";
        public const string LobbyMailFriend = "lobby.mail.friend";

        public const string FightChat = "fight.chat";
        public const string BagNewItem = "bag.new_item";

        #endregion

        /// <summary>「点进即消」类红点 key，如 tutorial.my_feature。</summary>
        public static string ForSeenFeature(string featureId) => $"seen.{featureId}";
    }
}
