using UnityEngine;

namespace Dragon.GameInput
{
    public interface IInputContextLifecycle
    {
        void Enter(PlayerInputAction.PlayerInputActions actions);
        void Exit(PlayerInputAction.PlayerInputActions actions);
    }

    public class InputContextBase
    {
        protected struct ButtonEdgeState
        {
            private bool _isHeld;
            private bool _pressedThisCycle;

            public bool IsHeld => _isHeld;

            public void HandlePressed()
            {
                if (!_isHeld)
                {
                    _pressedThisCycle = true;
                }

                _isHeld = true;
            }

            public void HandleReleased()
            {
                _isHeld = false;
            }

            public bool ConsumePressed()
            {
                bool pressed = _pressedThisCycle;
                _pressedThisCycle = false;
                return pressed;
            }

            public void Reset()
            {
                _isHeld = false;
                _pressedThisCycle = false;
            }
        }

    }
}
