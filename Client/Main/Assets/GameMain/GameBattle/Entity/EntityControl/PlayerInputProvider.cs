using System;
using UnityEngine;
using Dragon.GameInput;

public class PlayerInputProvider : IInputProvider
{
    private Func<Vector2> ReadMoveDirection;
    private Func<bool> ReadLeftRunPressed;
    private Func<bool> ReadRightRunPressed;
    private Func<bool> ReadJumpPressed;
    private Func<bool> ReadAttackPressed;

    public void Init()
    {
        ReadMoveDirection = InputManager.Instance.BattleInput.ReadMoveDirection;
        ReadLeftRunPressed = InputManager.Instance.BattleInput.ReadLeftRunPressed;
        ReadRightRunPressed = InputManager.Instance.BattleInput.ReadRightRunPressed;
        ReadJumpPressed = InputManager.Instance.BattleInput.ReadJumpPressed;
        ReadAttackPressed = InputManager.Instance.BattleInput.ReadAttackPressed;
    }

    public Vector2 GetMoveInput(Entity entity)
    {
        return ReadMoveDirection != null ? ReadMoveDirection() : Vector2.zero;
    }

    public bool GetJumpInput(Entity entity)
    {
        return ReadJumpPressed != null && ReadJumpPressed();
    }

    public bool GetAttackInput(Entity entity)
    {
        return ReadAttackPressed != null && ReadAttackPressed();
    }

    public bool GetLeftRunInput(Entity entity)
    {
        return ReadLeftRunPressed != null && ReadLeftRunPressed();
    }

    public bool GetRightRunInput(Entity entity)
    {
        return ReadRightRunPressed != null && ReadRightRunPressed();
    }
}
