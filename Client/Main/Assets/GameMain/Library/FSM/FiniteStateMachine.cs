using System.Collections.Generic;

namespace Dragon.Library
{
    //有限状态机
    public class FiniteStateMachine
    {
        #region //成员变量
        //当前状态机所有的状态
        private Dictionary<int, IState> _allState = new Dictionary<int, IState>();

        //状态机名字
        private string _name;

        //状态机所在宿主
        private object _host;

        //上一个状态
        private IState _prevState;

        //当前状态
        private IState _currentState;
        #endregion

        #region//属性
        //当前状态
        public IState CurrentState
        {
            get
            {
                return _currentState;
            }
        }

        //前一个状态
        public IState PrevState
        {
            get
            {
                return _prevState;
            }
        }

        //状态机所在宿主
        public object Host 
        { 
            get 
            { 
                return _host; 
            } 
            set 
            { 
                _host = value;
            }
        }

        public string Name 
        {
            get
            {
                return _name;
            }
        }

        #endregion


        public FiniteStateMachine(string name,object host)
        {
            _name = name;
            _host = host;
        }

        public void Register(IState state)
        {
            if (state != null)
            {
                _allState[state.Id] = state;                
                state.FSM = this;
            }
        }       
       
        public bool TransToState(int destId, object p)
        {
            //如果没有找到状态,就直接返回错误
            if (!_allState.TryGetValue(destId, out IState next))
            {
                return false;
            }

            if (_currentState != null)
            {
                if (!_currentState.IsCanTransTo(destId))
                {
                    return false;
                }

                _currentState.Exit();
                _prevState = _currentState;
                _currentState = null;
            }
            else
            {
                _prevState = null;
            }

            _currentState = next;
            _currentState.Enter(p);
            return true;
        }

        public void Update(float dt)
        {
            if (_currentState != null)
            {
                _currentState.Update(dt);
            }
        }

        public IState GetState(int id) {
            IState result;
            _allState.TryGetValue(id, out result);
            return result;
        }

        public void Clear() {
            _prevState = null;
            _currentState = null;
            foreach (var state in _allState) {
                state.Value.Destroy();
            }
            _allState.Clear();
        }

        public override string ToString()
        {
            return $"FSM[{_name}]::{_host}";
        }

    }
}
