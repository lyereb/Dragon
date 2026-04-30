using UnityEngine;

namespace Dragon.Library
{
    public abstract class BaseState : IState
    {
        //是否展示Log
        protected bool IsShowLog { get; set; } = false;
        //进入参数的对象
        private object _data = null;
        //逝去时间
        private float _elapseTime = 0;

        //判断当前阶段是否超时,-1的话表示永远都不会超时,0:表示下一帧立即切换
        private float _timeOut = -1;

        //超时时间
        public float TimeOut
        {
            get
            {
                return _timeOut;
            }
            set
            {
                _timeOut = value;
                _elapseTime = 0;
            }
        }

        public int Id
        {
            get
            {
                return OnStateId();
            }
        }

        public string Name
        {
            get
            {
                return OnStateName();
            }
        }

        public object Data
        {
            get
            {
                return _data;
            }
        }

        public FiniteStateMachine FSM { get; set; }

        public bool Enter(object data)
        {
            _data = data;
            if (IsShowLog) Debug.Log($"{FSM}.Enter:{Name}[{Id}],");
            return OnEnter(data);
        }

        public bool Exit()
        {
            if (IsShowLog) Debug.Log($"{FSM}.Exit:{Name}[{Id}]");
            return OnExit();
        }

        public bool IsCanTransTo(int destId)
        {
            var result = OnIsCanTransTo(destId);
            if (IsShowLog) Debug.Log($"{FSM}.IsCanTransTo:{Name}[{Id}]=>{destId}:::::result:{result}");
            return result;
        }
        public bool IsValid()
        {
            var result = OnIsValid();
            if (IsShowLog) Debug.Log($"{FSM}.IsValid:{Name}[{Id}]:::::result:{result}");
            return result;
        }

        public bool Update(float dt)
        {
            if (_timeOut >= 0)
            {
                _elapseTime += dt;
                if (_elapseTime >= _timeOut)
                {
                    _elapseTime = 0;
                    _timeOut = -1;
                    return OnTimeOut();
                }
            }
            return OnUpdate(dt);
        }

        public void Destroy()
        {
            OnDestroy();
        }

        protected abstract int OnStateId();
        protected abstract string OnStateName();
        protected abstract bool OnEnter(object data);
        protected abstract bool OnExit();
        protected abstract bool OnIsCanTransTo(int id);
        protected abstract bool OnIsValid();
        protected abstract bool OnUpdate(float dt);
        protected abstract bool OnTimeOut();
        protected abstract void OnDestroy();
    }
}
