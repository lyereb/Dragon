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
