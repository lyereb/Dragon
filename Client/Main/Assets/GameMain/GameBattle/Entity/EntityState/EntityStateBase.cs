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
        // 统一走宿主 ChangeToState，以触发 OnStateChangeBefore/After
        Host.ChangeToState(state, obj);
    }

    protected override bool OnIsCanTransTo(int id)
    {
        //如果状态不同
        return Id != id;
    }

    protected override void OnDestroy()
    {
        //销毁状态
    }
}
