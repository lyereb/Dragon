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

    protected override void OnEnter(object data)
    {
        UnityEngine.Debug.Log("Enter CharacterRunState");
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
