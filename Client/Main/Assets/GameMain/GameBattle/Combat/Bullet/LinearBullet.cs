/// <summary>
/// 直线子弹。在出生状态里沿发射方向移动，范围内有还没打过的目标就进入命中。
/// 穿透次数耗尽后死亡；小于 0 的穿透表示一直打到时间或距离结束。
/// </summary>
public class LinearBullet : BulletBase
{
    protected override void OnBirth()
    {
    }

    protected override void OnTickBirth(float dt)
    {
        Move(dt, out _);
        bool expired = IsExpired();
        bool outOfRange = IsOutOfRange();
        if (GatherHits() > 0)
        {
            ChangeToState(BulletStateCode.Hit);
            return;
        }

        if (outOfRange || expired)
        {
            ChangeToState(BulletStateCode.Death);
        }
    }

    protected override bool ShouldDespawnAfterHit()
    {
        return PierceRemaining == 0;
    }
}
