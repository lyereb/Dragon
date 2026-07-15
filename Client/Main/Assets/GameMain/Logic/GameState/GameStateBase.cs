using Dragon.Library;

namespace Dragon.Logic
{
    public abstract class GameStateBase : BaseState
    {
        public void ChangeToState(GameStateCode state, object obj = null)
        {
            FSM.TransToState((int)state, obj);
        }
        protected override void OnUpdate(float dt)
        {
        }
        protected override void OnDestroy()
        {
            //销毁状态
        }
    }
}
