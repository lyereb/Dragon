using Dragon.Library;
using System;
using UnityEngine;

namespace Dragon.Logic
{
    /// <summary>
    /// 游戏状态系统
    /// </summary>
    public class GameStateManager
    {
        #region//私有变量
        //所有状态集合
        private FiniteStateMachine _fsm = null;
        #endregion

        public GameStateManager()
        {
            _fsm = new FiniteStateMachine("GameState", this);
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize()
        {
            Debug.Log("GameStateManager:Initialize");
            try
            {
                _fsm.Register(new GameInitState());
                _fsm.Register(new GameUpdateState());
                _fsm.Register(new GamePreLoadState());
                _fsm.Register(new GameLoginState());
                _fsm.Register(new GameMainState());
                _fsm.Register(new GameLoadingState());
                _fsm.Register(new GameFightState());

                //最新进行资源预加载
                ChangeState(GameStateCode.Init);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// 反初始化
        /// </summary>
        public void Uninitialize()
        {
            Debug.Log("GameStateManager:Uninitialize");
            if (_fsm != null)
            {
                _fsm.Clear();
            }
        }

        /// <summary>
        /// 通过id获取游戏状态
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public BaseState GetCurState()
        {
            if (_fsm != null)
                return (BaseState)_fsm.CurrentState;
            return null;
        }

        public BaseState GetPreState()
        {
            if (_fsm != null)
                return (BaseState)_fsm.PrevState;
            return null;
        }

        public BaseState GetState(GameStateCode stateId)
        {
            if (_fsm != null)
                return (BaseState)_fsm.GetState((int)stateId);
            return null;
        }


        public bool IsCurState(GameStateCode stateId)
        {
            if (_fsm != null && _fsm.CurrentState != null)
            {
                return _fsm.CurrentState.Id == (int)stateId;
            }
            return false;
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        /// <param name="stateId"></param>
        /// <param name="arg"></param>
        public void ChangeState(GameStateCode stateId, object arg = null)
        {
            if (_fsm != null)
                _fsm.TransToState((int)stateId, arg);
        }

        /// <summary>
        /// 更新fsm
        /// </summary>
        /// <param name="dt"></param>
        public void Update(float dt)
        {
            if (_fsm != null)
                _fsm.Update(dt);
        }

        public void ReturnToLogin()
        {

        }
    }
}

