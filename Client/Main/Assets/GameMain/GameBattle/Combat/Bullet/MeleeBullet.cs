/// <summary>
/// 近战子弹。出生后停在生成点，立刻结算一次。
/// 单体还是群体由子弹表的 HitMode 决定，不拆成两个子类：
/// 两者的生命周期一样，差别只是这一次收几个目标。
/// </summary>
public class MeleeBullet : BulletBase
{
    protected override void OnBirth()
    {
        if (GatherHits() > 0)
        {
            ChangeToState(BulletStateCode.Hit);
            return;
        }

        ChangeToState(BulletStateCode.Death);
    }

    protected override bool ShouldDespawnAfterHit()
    {
        return true;
    }
}
