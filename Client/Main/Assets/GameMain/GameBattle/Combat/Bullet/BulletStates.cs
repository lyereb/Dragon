using Dragon.Library;

public abstract class BulletStateBase : BaseState
{
    public BulletBase Host
    {
        get { return (BulletBase)FSM.Host; }
    }

    protected override bool OnIsCanTransTo(int id)
    {
        if (Id == (int)BulletStateCode.Death)
        {
            return false;
        }

        return Id != id;
    }

    protected override void OnExit()
    {
    }

    protected override void OnTimeOut()
    {
    }

    protected override void OnDestroy()
    {
    }
}

/// <summary>
/// 出生同时是子弹的存续状态：近战在这里立刻选目标，直线弹在这里位移和检测。
/// </summary>
public class BulletBirthState : BulletStateBase
{
    protected override int OnStateId()
    {
        return (int)BulletStateCode.Birth;
    }

    protected override string OnStateName()
    {
        return BulletStateCode.Birth.ToString();
    }

    protected override void OnEnter(object data)
    {
        Host.EnterBirth();
    }

    protected override void OnUpdate(float dt)
    {
        Host.TickBirth(dt);
    }
}

public class BulletHitState : BulletStateBase
{
    protected override int OnStateId()
    {
        return (int)BulletStateCode.Hit;
    }

    protected override string OnStateName()
    {
        return BulletStateCode.Hit.ToString();
    }

    protected override void OnEnter(object data)
    {
        Host.EnterHit();
    }

    protected override void OnUpdate(float dt)
    {
    }
}

public class BulletDeathState : BulletStateBase
{
    protected override int OnStateId()
    {
        return (int)BulletStateCode.Death;
    }

    protected override string OnStateName()
    {
        return BulletStateCode.Death.ToString();
    }

    protected override void OnEnter(object data)
    {
        Host.EnterDeath();
    }

    protected override void OnUpdate(float dt)
    {
    }

    protected override bool OnIsCanTransTo(int id)
    {
        return false;
    }
}
