/// <summary>
/// 跳跃状态
/// </summary>
public class CharacterJumpState : EntityStateBase
{
    protected override int OnStateId()
    {
        return (int)EntityStateCode.Jump;
    }

    protected override string OnStateName()
    {
        return EntityStateCode.Jump.ToString();
    }

    protected override bool OnEnter(object data)
    {
        UnityEngine.Debug.Log("Enter CharacterJumpState");
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
