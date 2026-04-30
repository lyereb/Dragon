public class CharacterIdleState : EntityStateBase
{

    protected override int OnStateId()
    {
        return (int)EntityStateCode.Idle;
    }

    protected override string OnStateName()
    {
        return EntityStateCode.Idle.ToString();
    }

    protected override bool OnEnter(object data)
    {
        UnityEngine.Debug.Log("Enter CharacterIdleState");
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

