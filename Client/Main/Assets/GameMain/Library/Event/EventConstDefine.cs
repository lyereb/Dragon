namespace Dragon.Library
{
    /// <summary>
    /// 消息常量定义
    /// </summary>
    public class EventConstDefine
    {
        //核心基础消息ID
        public const int EVENT_MAIN_BASE_ID = 0;
        //UI消息的基础ID
        public const int EVENT_UI_BASE_ID = 100000;
        //战斗消息的基础ID
        public const int EVENT_BATTLE_BASE_ID = 200000;
    }

    /// <summary>
    /// 主消息定义
    /// </summary>
    public class MainEventDefine
    {
        //基础
        private const int EID_BASE = EventConstDefine.EVENT_MAIN_BASE_ID;
        /// <summary>
        /// 道具数量发生任何变化后派发；param 为 <see cref="Dragon.Logic.ItemChangeEventParam"/>。
        /// </summary>
        public const int EID_ITEM_INVENTORY_CHANGED = EID_BASE + 1;

        /// <summary>本地语言设置变更；无 param，读 <see cref="LocalSettingManager.Language"/>。</summary>
        public const int EID_LOCAL_SETTING_LANGUAGE_CHANGED = EID_BASE + 2;

        /// <summary>本地音量设置变更；无 param，读 <see cref="LocalSettingManager"/> 音量属性。</summary>
        public const int EID_LOCAL_SETTING_VOLUME_CHANGED = EID_BASE + 3;
    }

    /// <summary>
    /// UI消息定义
    /// </summary>
    public class UIEventDefine
    {
        //基础
        private const int EID_BASE = EventConstDefine.EVENT_UI_BASE_ID;

        /// <summary>红点数值变化；param 为 <see cref="Dragon.Logic.RedDotChangedEventParam"/>。</summary>
        public const int EID_REDDOT_CHANGED = EID_BASE + 1;
    }

    /// <summary>
    /// 逻辑消息定义
    /// </summary>
    public class BattleEventDefine
    {
        //基础
        private const int EID_BASE = EventConstDefine.EVENT_BATTLE_BASE_ID;
        public const int EID_BATTLE_ATTACK_EVENT = EID_BASE + 1;
    }
}
