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

    protected override bool OnEnter(object data)
    {
        UnityEngine.Debug.Log("Enter CharacterMoveState");
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
