public class EntityIdleState : EntityStateBase
{

    protected override int OnStateId()
    {
        return (int)EntityStateCode.Idle;
    }

    protected override string OnStateName()
    {
        return EntityStateCode.Idle.ToString();
    }

    protected override bool OnEnter(object data)
    {
        return true;
    }

    protected override bool OnExit()
    {
        return true;
    }


    protected override bool OnUpdate(float dt)
    {
        return true;
    }

    protected override bool OnTimeOut()
    {
        return true;
    }


}

