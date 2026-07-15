/// <summary>
/// 可交互物出生状态
/// </summary>
public class InteractableBirthState : EntityStateBase
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
