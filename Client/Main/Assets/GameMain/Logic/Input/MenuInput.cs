using UnityEngine;
using UnityEngine.InputSystem;

namespace Dragon.GameInput
{
    public class MenuInput : InputContextBase, IInputContextLifecycle
    {
        private const float NavigateDeadZone = 0.5f;
        private const float NavigateRepeatDelay = 0.35f;
        private const float NavigateRepeatInterval = 0.12f;

        private Vector2 _navigateInput;
        private Vector2 _navigateRepeatDirection;
        private float _nextNavigateTriggerTime;
        private ButtonEdgeState _confirmState;
        private ButtonEdgeState _backState;

        #region Lifecycle
        public void Enter(PlayerInputAction.PlayerInputActions actions)
        {
            Reset();
            actions.Move.performed += OnMovePerformed;
            actions.Move.canceled += OnMoveCanceled;
            actions.Attack.performed += OnConfirmPerformed;
            actions.Attack.canceled += OnConfirmCanceled;
            actions.Jump.performed += OnBackPerformed;
            actions.Jump.canceled += OnBackCanceled;
        }

        public void Exit(PlayerInputAction.PlayerInputActions actions)
        {
            actions.Move.performed -= OnMovePerformed;
            actions.Move.canceled -= OnMoveCanceled;
            actions.Attack.performed -= OnConfirmPerformed;
            actions.Attack.canceled -= OnConfirmCanceled;
            actions.Jump.performed -= OnBackPerformed;
            actions.Jump.canceled -= OnBackCanceled;
            Reset();
        }
        #endregion

        #region Navigate
        public Vector2 ReadMenuNavigate()
        {
            return _navigateInput;
        }

        public bool ReadMenuNavigatePressed()
        {
            Vector2 currentDirection = GetNavigateDirection(_navigateInput);
            if (currentDirection == Vector2.zero)
            {
                _navigateRepeatDirection = Vector2.zero;
                _nextNavigateTriggerTime = 0f;
                return false;
            }

            if (currentDirection != _navigateRepeatDirection)
            {
                _navigateRepeatDirection = currentDirection;
                _nextNavigateTriggerTime = Time.unscaledTime + NavigateRepeatDelay;
                return true;
            }

            if (Time.unscaledTime >= _nextNavigateTriggerTime)
            {
                _nextNavigateTriggerTime = Time.unscaledTime + NavigateRepeatInterval;
                return true;
            }

            return false;
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            _navigateInput = context.ReadValue<Vector2>();
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            _navigateInput = Vector2.zero;
            _navigateRepeatDirection = Vector2.zero;
            _nextNavigateTriggerTime = 0f;
        }

        private static Vector2 GetNavigateDirection(Vector2 input)
        {
            if (input.sqrMagnitude < NavigateDeadZone * NavigateDeadZone)
            {
                return Vector2.zero;
            }

            if (Mathf.Abs(input.x) >= Mathf.Abs(input.y))
            {
                return new Vector2(Mathf.Sign(input.x), 0f);
            }

            return new Vector2(0f, Mathf.Sign(input.y));
        }
        #endregion

        #region Confirm
        public bool ReadMenuConfirmPressed()
        {
            return _confirmState.ConsumePressed();
        }

        public bool ReadMenuConfirmHeld()
        {
            return _confirmState.IsHeld;
        }

        private void OnConfirmPerformed(InputAction.CallbackContext context)
        {
            if (context.ReadValueAsButton())
            {
                _confirmState.HandlePressed();
            }
        }

        private void OnConfirmCanceled(InputAction.CallbackContext context)
        {
            _confirmState.HandleReleased();
        }
        #endregion

        #region Back
        public bool ReadMenuBackPressed()
        {
            return _backState.ConsumePressed();
        }

        public bool ReadMenuBackHeld()
        {
            return _backState.IsHeld;
        }

        private void OnBackPerformed(InputAction.CallbackContext context)
        {
            if (context.ReadValueAsButton())
            {
                _backState.HandlePressed();
            }
        }

        private void OnBackCanceled(InputAction.CallbackContext context)
        {
            _backState.HandleReleased();
        }
        #endregion

        #region State
        private void Reset()
        {
            _navigateInput = Vector2.zero;
            _navigateRepeatDirection = Vector2.zero;
            _nextNavigateTriggerTime = 0f;
            _confirmState.Reset();
            _backState.Reset();
        }
        #endregion
    }
}
