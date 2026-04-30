using UnityEngine;
using UnityEngine.InputSystem;

namespace Dragon.GameInput
{
    public class BattleInput : InputContextBase, IInputContextLifecycle
    {
        private const float RunDoublePressWindow = 0.3f;

        private Vector2 _moveInput;
        private ButtonEdgeState _jumpState;
        private ButtonEdgeState _attackState;
        private bool _leftRunPerformed;
        private bool _rightRunPerformed;
        private float _lastLeftRunPressTime;
        private float _lastRightRunPressTime;

        #region Lifecycle
        public void Enter(PlayerInputAction.PlayerInputActions actions)
        {
            Reset();
            actions.Move.performed += OnMovePerformed;
            actions.Move.canceled += OnMoveCanceled;
            actions.Jump.performed += OnJumpPerformed;
            actions.Jump.canceled += OnJumpCanceled;
            actions.Attack.performed += OnAttackPerformed;
            actions.Attack.canceled += OnAttackCanceled;
            actions.LeftRun.performed += OnLeftRunPerformed;
            actions.RightRun.performed += OnRightRunPerformed;
        }

        public void Exit(PlayerInputAction.PlayerInputActions actions)
        {
            actions.Move.performed -= OnMovePerformed;
            actions.Move.canceled -= OnMoveCanceled;
            actions.Jump.performed -= OnJumpPerformed;
            actions.Jump.canceled -= OnJumpCanceled;
            actions.Attack.performed -= OnAttackPerformed;
            actions.Attack.canceled -= OnAttackCanceled;
            actions.LeftRun.performed -= OnLeftRunPerformed;
            actions.RightRun.performed -= OnRightRunPerformed;
            Reset();
        }
        #endregion

        #region Move
        public Vector2 ReadMoveDirection()
        {
            return _moveInput;
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            _moveInput = Vector2.zero;
        }
        #endregion

        #region Jump
        public bool ReadJumpPressed()
        {
            return _jumpState.ConsumePressed();
        }

        public bool ReadJumpHeld()
        {
            return _jumpState.IsHeld;
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (context.ReadValueAsButton())
            {
                _jumpState.HandlePressed();
            }
        }

        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
            _jumpState.HandleReleased();
        }
        #endregion

        #region Attack
        public bool ReadAttackPressed()
        {
            return _attackState.ConsumePressed();
        }

        public bool ReadAttackHeld()
        {
            return _attackState.IsHeld;
        }

        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            if (context.ReadValueAsButton())
            {
                _attackState.HandlePressed();
            }
        }

        private void OnAttackCanceled(InputAction.CallbackContext context)
        {
            _attackState.HandleReleased();
        }
        #endregion

        #region Run
        public bool ReadLeftRunPressed()
        {
            bool performed = _leftRunPerformed;
            _leftRunPerformed = false;
            return performed;
        }

        public bool ReadRightRunPressed()
        {
            bool performed = _rightRunPerformed;
            _rightRunPerformed = false;
            return performed;
        }

        private void OnLeftRunPerformed(InputAction.CallbackContext context)
        {
            if (!context.ReadValueAsButton())
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now - _lastLeftRunPressTime <= RunDoublePressWindow)
            {
                _leftRunPerformed = true;
                _lastLeftRunPressTime = float.NegativeInfinity;
                return;
            }

            _lastLeftRunPressTime = now;
        }

        private void OnRightRunPerformed(InputAction.CallbackContext context)
        {
            if (!context.ReadValueAsButton())
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now - _lastRightRunPressTime <= RunDoublePressWindow)
            {
                _rightRunPerformed = true;
                _lastRightRunPressTime = float.NegativeInfinity;
                return;
            }

            _lastRightRunPressTime = now;
        }
        #endregion

        #region State
        private void Reset()
        {
            _moveInput = Vector2.zero;
            _jumpState.Reset();
            _attackState.Reset();
            _leftRunPerformed = false;
            _rightRunPerformed = false;
            _lastLeftRunPressTime = float.NegativeInfinity;
            _lastRightRunPressTime = float.NegativeInfinity;
        }
        #endregion
    }
}
