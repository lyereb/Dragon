/// <summary>
/// 死亡状态
/// </summary>
public class CharacterDeathState : EntityStateBase
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
        UnityEngine.Debug.Log("Enter CharacterDeathState");
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
