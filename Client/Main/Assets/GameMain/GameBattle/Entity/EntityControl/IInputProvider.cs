using UnityEngine;
public interface IInputProvider
{
    Vector2 GetMoveInput(Entity entity);
    bool GetLeftRunInput(Entity entity);
    bool GetRightRunInput(Entity entity);
    bool GetJumpInput(Entity entity);
    bool GetAttackInput(Entity entity);
}
