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

        protected override void OnEnter(object data)
        {
            UnityEngine.Debug.Log("GameUpdateState:OnEnter");
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
