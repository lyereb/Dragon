/// <summary>
/// 可交互物死亡状态
/// </summary>
public class InteractableDeathState : EntityStateBase
{
    protected override int OnStateId()
    {
        return (int)EntityStateCode.Death;
    }

    protected override string OnStateName()
    {
        return EntityStateCode.Death.ToString();
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
