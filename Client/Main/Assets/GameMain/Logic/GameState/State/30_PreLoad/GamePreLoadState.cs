using UnityEngine;

namespace Dragon.Logic
{
    /// <summary>
    /// 预加载状态
    /// </summary>
    public class GamePreLoadState : GameStateBase
    {
        #region//变量

        #endregion


        #region//重写父类BaseState的函数
        protected override int OnStateId()
        {
            return (int)GameStateCode.PreLoad;
        }

        protected override string OnStateName()
        {
            return GameStateCode.PreLoad.ToString();
        }


        protected override void OnEnter(object data)
        {
            UnityEngine.Debug.Log("GamePreLoadState:OnEnter");
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
