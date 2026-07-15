/// <summary>
/// 移动状态
/// </summary>
public class CharacterMoveState : EntityStateBase
{
    protected override int OnStateId()
    {
        return (int)EntityStateCode.Move;
    }

    protected override string OnStateName()
    {
        return EntityStateCode.Move.ToString();
    }

    protected override void OnEnter(object data)
    {
        UnityEngine.Debug.Log("Enter CharacterMoveState");
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
