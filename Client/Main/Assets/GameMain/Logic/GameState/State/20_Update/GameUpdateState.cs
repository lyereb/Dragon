namespace Dragon.Logic
{
    public class GameUpdateState : GameStateBase
    {
        #region//变量

        #endregion

        #region//重写父类BaseState的函数
        protected override int OnStateId()
        {
            return (int)GameStateCode.Update;
        }

        protected override string OnStateName()
        {
            return GameStateCode.Update.ToString();
        }

        protected override bool OnEnter(object data)
        {            
            return true;
        }

        protected override bool OnExit()
        {
            return true;
        }

        protected override bool OnUpdate(float deltaTime)
        {
            return base.OnUpdate(deltaTime);
        }

        protected override bool OnIsCanTransTo(int id)
        {
            return true;
        }
        protected override bool OnTimeOut()
        {
            return true;
        }

        #endregion
    }
}