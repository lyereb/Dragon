using Dragon.Library;
public abstract class EntityStateBase : BaseState
{
    public Entity Host
    {
        get
        {
            return (Entity)(FSM.Host);
        }
    }

    public void ChangeToState(EntityStateCode state, object obj = null)
    {
        bool result = FSM.TransToState((int)state, obj);
    }

    protected override bool OnIsCanTransTo(int id)
    {
        //如果状态不同
        return Id != id;
    }

    protected override bool OnIsValid()
    {
        return true;
    }

    protected override void OnDestroy()
    {
        //销毁状态
    }
}