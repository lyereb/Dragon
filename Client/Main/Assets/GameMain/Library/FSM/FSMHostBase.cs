using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// 状态机主机基类
    /// </summary>
    /// <typeparam name="T">状态枚举类型（约定底层为 int）</typeparam>
    public class FSMHostBase<T> where T : unmanaged, Enum
    {
        protected FiniteStateMachine _fsm = null;

        public FiniteStateMachine FSM { get { return _fsm; } }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">状态机名称</param>
        public FSMHostBase(string name)
        {
            _fsm = new FiniteStateMachine(name, this);
            if (!OnFSMSetup())
            {
                Debug.LogError("Initialize Fail,OnFSMSetup return false!");
            }
        }

        /// <summary>
        /// 销毁
        /// </summary>
        public void DestroyFSM()
        {
            if (_fsm != null)
            {
                _fsm.Clear();
                _fsm = null;
            }
        }

        //是否可以切换到目标状态
        public bool IsCanTrans(T code)
        {
            //状态切换判断      
            if (_fsm != null)
            {
                var cur = _fsm.CurrentState;
                if (cur != null && cur.IsCanTransTo(ToStateId(code)))
                {
                    return true;
                }
            }
            return false;
        }
        //是否是当前状态
        public bool IsCurrentState(T code)
        {
            if (_fsm != null && _fsm.CurrentState != null)
            {
                return (_fsm.CurrentState.Id == ToStateId(code));
            }
            return false;
        }

        //是否是当前状态
        public bool IsCurrentStates(params T[] codes)
        {
            if (_fsm != null && _fsm.CurrentState != null)
            {
                int currentId = _fsm.CurrentState.Id;
                for (int i = 0; i < codes.Length; i++)
                {
                    if (currentId == ToStateId(codes[i]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool ChangeToState(T code, object data = null)
        {
            if (_fsm != null)
            {
                OnStateChangeBefore(code);
                bool result = _fsm.TransToState(ToStateId(code), data);
                OnStateChangeAfter(code, result);
                return result;
            }
            return false;
        }

        public string GetCurrentStateName()
        {
            if (_fsm != null && _fsm.CurrentState != null)
            {
                return _fsm.CurrentState.Name;
            }
            return "null";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ToStateId(T code)
        {
            return UnsafeUtility.EnumToInt(code);
        }

        protected virtual bool OnFSMSetup()
        {
            return true;
        }

        protected virtual void OnFSMUpdate(float dt)
        {
            if (_fsm != null)
            {
                _fsm.Update(dt);
            }
        }
        protected virtual void OnStateChangeBefore(T code)
        {

        }

        protected virtual void OnStateChangeAfter(T code, bool isSuccess)
        {

        }
    }

}
