using UnityEngine;

namespace Dragon.GameInput
{
    public class InputManager
    {
        public enum InputContextState
        {
            Disabled = 0,
            Menu = 1,
            Battle = 2
        }

        #region //静态变量

        //静态变量
        protected static InputManager _instance = null;

        //静态属性
        public static InputManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new InputManager();
                }
                return _instance;
            }
        }
        #endregion

        private PlayerInputAction _playerInputAction;
        private readonly BattleInput _battleInput = new BattleInput();
        private readonly MenuInput _menuInput = new MenuInput();
        private readonly DisabledInput _disabledInput = new DisabledInput();
        private IInputContextLifecycle _activeInputContext;
        private InputContextState _currentState = InputContextState.Disabled;

        public InputContextState CurrentState => _currentState;
        public BattleInput BattleInput => _battleInput;
        public MenuInput MenuInput => _menuInput;
        public DisabledInput DisabledInput => _disabledInput;

        public void Init()
        {
            if (_playerInputAction != null)
            {
                return;
            }

            _playerInputAction = new PlayerInputAction();
            _playerInputAction.PlayerInput.Enable();
            SetInputContext(InputContextState.Disabled);
        }

        public void Shutdown()
        {
            if (_playerInputAction == null)
            {
                return;
            }

            if (_activeInputContext != null)
            {
                _activeInputContext.Exit(_playerInputAction.PlayerInput);
                _activeInputContext = null;
            }

            _playerInputAction.PlayerInput.Disable();
            _playerInputAction.Dispose();
            _playerInputAction = null;
            _currentState = InputContextState.Disabled;
        }

        public void SetInputContext(InputContextState state)
        {
            if (_playerInputAction == null)
            {
                _currentState = state;
                return;
            }

            if (_activeInputContext != null && _currentState == state)
            {
                return;
            }

            if (_activeInputContext != null)
            {
                _activeInputContext.Exit(_playerInputAction.PlayerInput);
            }

            _currentState = state;
            _activeInputContext = GetInputContextByState(state);
            _activeInputContext.Enter(_playerInputAction.PlayerInput);
        }

        private IInputContextLifecycle GetInputContextByState(InputContextState state)
        {
            switch (state)
            {
                case InputContextState.Battle:
                    return _battleInput;
                case InputContextState.Menu:
                    return _menuInput;
                default:
                    return _disabledInput;
            }
        }
    }
}