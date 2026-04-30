using Dragon.Library;

namespace Dragon.Logic
{
    public abstract class GameStateBase : BaseState
    {
        public void ChangeToState(GameStateCode state, object obj = null)
        {
            FSM.TransToState((int)state, obj);
        }
        protected override bool OnUpdate(float dt)
        {
            return true;
        }
        protected override void OnDestroy()
        {
            //销毁状态
        }

        protected override bool OnIsValid()
        {
            return true;
        }
    }
}
