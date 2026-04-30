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

    protected override bool OnEnter(object data)
    {
        UnityEngine.Debug.Log("Enter CharacterDeathState");
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
