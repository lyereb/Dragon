using Dragon.Library;

public abstract class EmitterStateBase : BaseState
{
    public EmitterBase Host
    {
        get { return (EmitterBase)FSM.Host; }
    }

    protected override bool OnIsCanTransTo(int id)
    {
        if (Id == (int)EmitterStateCode.Death)
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

public class EmitterBirthState : EmitterStateBase
{
    protected override int OnStateId()
    {
        return (int)EmitterStateCode.Birth;
    }

    protected override string OnStateName()
    {
        return EmitterStateCode.Birth.ToString();
    }

    protected override void OnEnter(object data)
    {
        float duration = Host.Config.BirthDuration;
        if (duration <= 0f)
        {
            Host.ChangeToState(EmitterStateCode.Ready);
            return;
        }

        TimeOut = duration;
    }

    protected override void OnUpdate(float dt)
    {
    }

    protected override void OnTimeOut()
    {
        Host.ChangeToState(EmitterStateCode.Ready);
    }
}

public class EmitterReadyState : EmitterStateBase
{
    protected override int OnStateId()
    {
        return (int)EmitterStateCode.Ready;
    }

    protected override string OnStateName()
    {
        return EmitterStateCode.Ready.ToString();
    }

    protected override void OnEnter(object data)
    {
    }

    protected override void OnUpdate(float dt)
    {
        Host.TickReady(dt);
    }
}

public class EmitterFireState : EmitterStateBase
{
    protected override int OnStateId()
    {
        return (int)EmitterStateCode.Fire;
    }

    protected override string OnStateName()
    {
        return EmitterStateCode.Fire.ToString();
    }

    protected override void OnEnter(object data)
    {
        Host.EnterFire();
    }

    protected override void OnUpdate(float dt)
    {
        Host.TickFire(dt);
    }
}

public class EmitterDeathState : EmitterStateBase
{
    protected override int OnStateId()
    {
        return (int)EmitterStateCode.Death;
    }

    protected override string OnStateName()
    {
        return EmitterStateCode.Death.ToString();
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
