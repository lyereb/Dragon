/// <summary>
/// 出生状态
/// </summary>
public class CharacterBirthState : EntityStateBase
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
        UnityEngine.Debug.Log("Enter CharacterBirthState");
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
