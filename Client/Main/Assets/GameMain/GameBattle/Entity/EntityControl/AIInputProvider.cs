using System;
using UnityEngine;

public class AIInputProvider : IInputProvider
{
    public void Init()
    {

    }

    public Vector2 GetMoveInput(Entity entity)
    {
        return Vector2.zero;
    }

    public bool GetLeftRunInput(Entity entity)
    {
        return false;
    }

    public bool GetRightRunInput(Entity entity)
    {
        return false;
    }

    public bool GetJumpInput(Entity entity)
    {
        return false;
    }

    public bool GetAttackInput(Entity entity)
    {
        return false;
    }
}
