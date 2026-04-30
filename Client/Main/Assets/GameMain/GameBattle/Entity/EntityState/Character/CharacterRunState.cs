/// <summary>
/// 奔跑状态
/// </summary>
public class CharacterRunState : EntityStateBase
{
    protected override int OnStateId()
    {
        return (int)EntityStateCode.Run;
    }

    protected override string OnStateName()
    {
        return EntityStateCode.Run.ToString();
    }

    protected override bool OnEnter(object data)
    {
        UnityEngine.Debug.Log("Enter CharacterRunState");
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
