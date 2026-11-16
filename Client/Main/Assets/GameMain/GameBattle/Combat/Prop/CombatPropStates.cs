/// <summary>
/// 战斗物件出生。前摇结束或时长为 0 时进入持续。
/// </summary>
public class CombatPropBirthState : EntityStateBase
{
    protected override int OnStateId()
    {
        return (int)EntityStateCode.Birth;
    }

    protected override string OnStateName()
    {
        return EntityStateCode.Birth.ToString();
    }

    protected override void OnEnter(object data)
    {
        CombatProp prop = (CombatProp)Host;
        float duration = prop.Config != null ? prop.Config.BirthDuration : 0f;
        if (duration <= 0f)
        {
            ChangeToState(EntityStateCode.Idle);
            return;
        }

        TimeOut = duration;
    }

    protected override void OnExit()
    {
    }

    protected override void OnUpdate(float dt)
    {
    }

    protected override void OnTimeOut()
    {
        ChangeToState(EntityStateCode.Idle);
    }
}

/// <summary>
/// 战斗物件持续。发射器、区域检测和存活时间在物件自己的更新里跑。
/// </summary>
public class CombatPropIdleState : EntityStateBase
{
    protected override int OnStateId()
    {
        return (int)EntityStateCode.Idle;
    }

    protected override string OnStateName()
    {
        return EntityStateCode.Idle.ToString();
    }

    protected override void OnEnter(object data)
    {
    }

    protected override void OnExit()
    {
    }

    protected override void OnUpdate(float dt)
    {
    }

    protected override void OnTimeOut()
    {
    }
}

/// <summary>
/// 战斗物件死亡。卸掉自己挂上的区域 Buff，并等待管理器移除。
/// </summary>
public class CombatPropDeathState : EntityStateBase
{
    protected override int OnStateId()
    {
        return (int)EntityStateCode.Death;
    }

    protected override string OnStateName()
    {
        return EntityStateCode.Death.ToString();
    }

    protected override void OnEnter(object data)
    {
        ((CombatProp)Host).EnterDeath();
    }

    protected override void OnExit()
    {
    }

    protected override void OnUpdate(float dt)
    {
    }

    protected override void OnTimeOut()
    {
    }

    protected override bool OnIsCanTransTo(int id)
    {
        return false;
    }
}
