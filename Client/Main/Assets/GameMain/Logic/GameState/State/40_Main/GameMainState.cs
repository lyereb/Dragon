using Dragon.GameUI;
using Dragon.GameInput;
namespace Dragon.Logic
{
    /// <summary>
    /// 主界面状态
    /// </summary>
    public class GameMainState : GameStateBase
    {

        #region//变量

        #endregion


        #region//重写父类BaseState的函数
        protected override int OnStateId()
        {
            return (int)GameStateCode.Main;
        }

        protected override string OnStateName()
        {
            return GameStateCode.Main.ToString();
        }


        protected override bool OnEnter(object data)
        {
            InputManager.Instance.SetInputContext(InputManager.InputContextState.Menu);
            _ = GameUIManager.Instance.OpenMainUI<UILobby, UILobbyData>("main_lobby", new UILobbyData());
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
