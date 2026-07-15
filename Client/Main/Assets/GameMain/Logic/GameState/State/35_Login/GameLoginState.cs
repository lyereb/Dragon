namespace Dragon.Logic
{
    /// <summary>
    /// 登录状态
    /// </summary>
    public class GameLoginState : GameStateBase
    {
        #region//变量

        #endregion


        #region//重写父类BaseState的函数
        protected override int OnStateId()
        {
            return (int)GameStateCode.Login;
        }

        protected override string OnStateName()
        {
            return GameStateCode.Login.ToString();
        }


        protected override void OnEnter(object data)
        {
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
