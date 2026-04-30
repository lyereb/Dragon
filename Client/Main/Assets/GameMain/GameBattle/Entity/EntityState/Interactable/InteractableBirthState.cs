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
