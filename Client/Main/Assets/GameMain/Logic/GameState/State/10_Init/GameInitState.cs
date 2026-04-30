using Dragon.GameUI;

namespace Dragon.Logic
{

    public class GameInitState: GameStateBase
    {
        #region//变量

        #endregion

        #region//重写父类BaseState的函数
        protected override int OnStateId()
        {
            return (int)GameStateCode.Init;
        }

        protected override string OnStateName()
        {
            return GameStateCode.Init.ToString();
        }


        protected override bool OnEnter(object data)
        {
            TimeOut = 1;
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
