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


        protected override void OnEnter(object data)
        {
            UnityEngine.Debug.Log("GameInitState:OnEnter");
        }

        protected override void OnExit()
        {
        }

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);
        }

        protected override bool OnIsCanTransTo(int id)
        {
            return true;
        }
        protected override void OnTimeOut()
        {
        }

        #endregion

    }
}
