using Dragon.Library;
using Dragon.GameUI;
using Dragon.GameInput;

namespace Dragon.Logic
{
    /// <summary>
    /// 场景切换状态
    /// </summary>
    public class GameLoadingState : GameStateBase
    {
        #region//变量
        private GameLoadingParam _param;
        #endregion

        #region//重写父类BaseState的函数
        protected override int OnStateId()
        {
            return (int)GameStateCode.Loading;
        }

        protected override string OnStateName()
        {
            return GameStateCode.Loading.ToString();
        }


        protected override bool OnEnter(object data)
        {
            InputManager.Instance.SetInputContext(InputManager.InputContextState.Disabled);
            _param = (GameLoadingParam)data;

            _ = GameUIManager.Instance.OpenMainUI<UILoading, UILoadingData>("main_loading", new UILoadingData() { LoadingText = "Loading..." });
            TimeOut = _param.LoadingTime;
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
            ChangeToState(_param.TargetState);
            return true;
        }
        #endregion

    }
}