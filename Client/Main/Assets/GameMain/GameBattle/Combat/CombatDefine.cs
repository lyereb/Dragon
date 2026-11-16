/// <summary>
/// 阵营常量。玩家、敌对、中立是默认三组，玩法可以继续使用其它正整数。
/// </summary>
public static class CombatFaction
{
    public const int Neutral = 0;
    public const int Player = 1;
    public const int Enemy = 2;
}

public enum EmitterStateCode
{
    Birth = 1,
    Ready = 2,
    Fire = 3,
    Death = 4,
}

public enum BulletStateCode
{
    Birth = 1,
    Hit = 2,
    Death = 3,
}

public enum BuffRemoveReason
{
    Expired = 1,
    Replaced = 2,
    Dispelled = 3,
    OwnerGone = 4,
    /// <summary>施加来源结束，例如离开区域或区域物件销毁。</summary>
    SourceEnded = 5,
}
