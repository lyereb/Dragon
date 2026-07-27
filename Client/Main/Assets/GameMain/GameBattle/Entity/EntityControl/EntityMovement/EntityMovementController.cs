using UnityEngine;
using Dragon.Logic;
using cfg.entity.dragon;

public class EntityMovementController
{
    private float _verticalVelocity;
    private bool _isRunBoostActive;
    private EntityStateCode _lastMovementState = EntityStateCode.Idle;

    public void Update(Entity entity, IInputProvider inputProvider, float dt)
    {
        if (entity == null || inputProvider == null || dt <= 0f)
        {
            return;
        }

        CharacterData characterData = entity.GetData<CharacterData>();
        if (characterData == null)
        {
            return;
        }

        Vector3 currentLogicPos = entity.Data.Position.LogicPosition;

        Vector2 moveInput = inputProvider.GetMoveInput(entity);
        bool hasMoveInput = moveInput.sqrMagnitude > 0f;

        if (!hasMoveInput)
        {
            _isRunBoostActive = false;
        }

        bool leftRunPressed = inputProvider.GetLeftRunInput(entity);
        bool rightRunPressed = inputProvider.GetRightRunInput(entity);
        if (hasMoveInput && (leftRunPressed || rightRunPressed))
        {
            _isRunBoostActive = true;
        }

        // Logic plane: X = horizontal, Y = depth.
        Vector3 moveDirection = new Vector3(moveInput.x, moveInput.y, 0f);
        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        float moveSpeed = characterData.GetCurrentAttribute(CharacterAttr.MOVE_SPEED);
        Vector3 moveDelta = hasMoveInput && moveSpeed > 0f ? moveDirection * moveSpeed * dt : Vector3.zero;
        if (_isRunBoostActive)
        {
            moveDelta.x *= GlobalDefine.BattleRunSpeedMultiplierX;
        }

        bool jumpPressed = inputProvider.GetJumpInput(entity);
        bool isGrounded = currentLogicPos.z <= 0f;
        float verticalVelocity = _verticalVelocity;
        if (isGrounded)
        {
            if (jumpPressed && characterData.GetCurrentAttribute(CharacterAttr.JUMP_VELOCITY) > 0f)
            {
                verticalVelocity = characterData.GetCurrentAttribute(CharacterAttr.JUMP_VELOCITY);
            }
            else if (verticalVelocity < 0f)
            {
                verticalVelocity = 0f;
            }
        }

        float gravity = GlobalDefine.BattleGravity > 0f ? GlobalDefine.BattleGravity : 0f;
        verticalVelocity -= gravity * dt;
        float nextZ = currentLogicPos.z + verticalVelocity * dt;
        if (nextZ <= 0f)
        {
            nextZ = 0f;
            if (verticalVelocity < 0f)
            {
                verticalVelocity = 0f;
            }
        }
        _verticalVelocity = verticalVelocity;

        EntityStateCode movementState = ResolveMovementState(hasMoveInput, isGrounded, _isRunBoostActive);
        UpdateCharacterState(entity, movementState);

        float nextX = currentLogicPos.x + moveDelta.x;
        float nextY = ClampDepthInMapBoundary(entity, currentLogicPos.y + moveDelta.y);
        ResolveBlockByRigidbodies(entity, currentLogicPos, ref nextX, ref nextY);
        entity.Data.Position.SetLogicPosition(nextX, nextY, nextZ);
    }

    public void ClearState()
    {
        _verticalVelocity = 0f;
        _isRunBoostActive = false;
        _lastMovementState = EntityStateCode.Idle;
    }

    private static EntityStateCode ResolveMovementState(bool hasMoveInput, bool isGrounded, bool isRunBoostActive)
    {
        if (!isGrounded)
        {
            return EntityStateCode.Jump;
        }

        if (!hasMoveInput)
        {
            return EntityStateCode.Idle;
        }

        return isRunBoostActive ? EntityStateCode.Run : EntityStateCode.Move;
    }

    private void UpdateCharacterState(Entity entity, EntityStateCode movementState)
    {
        if (entity is not Character)
        {
            return;
        }

        if (_lastMovementState == movementState)
        {
            return;
        }

        entity.ChangeToState(movementState);
        _lastMovementState = movementState;
    }

    private float ClampDepthInMapBoundary(Entity entity, float targetY)
    {
        MapData mapData = MapSceneManager.Instance.MapData;
        if (mapData == null)
        {
            return targetY;
        }

        float lowerLimit = mapData.MapLowerLimit;
        float upperLimit = mapData.MapUpperLimit;
        if (upperLimit < lowerLimit)
        {
            return targetY;
        }

        // Keep entity's depth thickness inside map bounds.
        float depthHalfSize = Mathf.Max(0f, entity != null ? entity.Data.DepthSize * 0.5f : 0f);
        float minY = lowerLimit + depthHalfSize;
        float maxY = upperLimit - depthHalfSize;
        if (maxY < minY)
        {
            float mid = (lowerLimit + upperLimit) * 0.5f;
            return mid;
        }

        return Mathf.Clamp(targetY, minY, maxY);
    }

    private void ResolveBlockByRigidbodies(Entity entity, Vector3 currentLogicPos, ref float targetX, ref float targetY)
    {
        if (entity == null)
        {
            return;
        }

        EntityManager entityManager = MapSceneManager.Instance.EntityManager;
        if (entityManager == null)
        {
            return;
        }

        if (!entityManager.IsBlockedByRigidbody(entity, targetX, targetY))
        {
            return;
        }

        float xOnlyTarget = targetX;
        float xOnlyDepth = currentLogicPos.y;
        bool canMoveOnXOnly = !entityManager.IsBlockedByRigidbody(entity, xOnlyTarget, xOnlyDepth);
        if (canMoveOnXOnly)
        {
            targetY = xOnlyDepth;
            return;
        }

        float yOnlyTarget = currentLogicPos.x;
        float yOnlyDepth = targetY;
        bool canMoveOnYOnly = !entityManager.IsBlockedByRigidbody(entity, yOnlyTarget, yOnlyDepth);
        if (canMoveOnYOnly)
        {
            targetX = yOnlyTarget;
            return;
        }

        targetX = currentLogicPos.x;
        targetY = currentLogicPos.y;
    }
}
