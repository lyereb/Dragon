using Dragon.GameUI;
using Dragon.GameInput;

namespace Dragon.Logic
{
    /// <summary>
    /// 战斗状态
    /// </summary>
    public class GameFightState : GameStateBase
    {
        #region//变量

        #endregion

        #region//重写父类BaseState的函数
        protected override int OnStateId()
        {
            return (int)GameStateCode.Fight;
        }


        protected override string OnStateName()
        {
            return GameStateCode.Fight.ToString();
        }

        protected override void OnEnter(object data)
        {
            InputManager.Instance.SetInputContext(InputManager.InputContextState.Battle);
            _ = GameUIManager.Instance.OpenMainUI<UIFight, UIFightData>("main_fight", new UIFightData());
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
