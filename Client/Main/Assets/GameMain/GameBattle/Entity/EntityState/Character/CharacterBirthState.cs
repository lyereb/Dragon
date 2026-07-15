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

    protected override void OnEnter(object data)
    {
        UnityEngine.Debug.Log("Enter CharacterBirthState");
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
