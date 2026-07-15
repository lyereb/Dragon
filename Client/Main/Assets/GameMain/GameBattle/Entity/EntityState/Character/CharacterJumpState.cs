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

    protected override void OnEnter(object data)
    {
        UnityEngine.Debug.Log("Enter CharacterJumpState");
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
