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
