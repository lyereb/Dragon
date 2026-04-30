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

        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);
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
        bool isGrounded = currentLogicPos.y <= 0f;
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
        float nextY = currentLogicPos.y + verticalVelocity * dt;
        if (nextY <= 0f)
        {
            nextY = 0f;
            if (verticalVelocity < 0f)
            {
                verticalVelocity = 0f;
            }
        }
        _verticalVelocity = verticalVelocity;

        EntityStateCode movementState = ResolveMovementState(hasMoveInput, isGrounded, _isRunBoostActive);
        UpdateCharacterState(entity, movementState);

        float nextX = currentLogicPos.x + moveDelta.x;
        float nextZ = ClampDepthInMapBoundary(entity, currentLogicPos.z + moveDelta.z);
        ResolveBlockByRigidbodies(entity, currentLogicPos, ref nextX, ref nextZ);
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

    private float ClampDepthInMapBoundary(Entity entity, float targetZ)
    {
        MapData mapData = MapSceneManager.Instance.MapData;
        if (mapData == null)
        {
            return targetZ;
        }

        float lowerLimit = mapData.MapLowerLimit;
        float upperLimit = mapData.MapUpperLimit;
        if (upperLimit < lowerLimit)
        {
            return targetZ;
        }

        // Keep entity's depth thickness inside map bounds.
        float depthHalfSize = Mathf.Max(0f, entity != null ? entity.Data.DepthSize * 0.5f : 0f);
        float minZ = lowerLimit + depthHalfSize;
        float maxZ = upperLimit - depthHalfSize;
        if (maxZ < minZ)
        {
            float mid = (lowerLimit + upperLimit) * 0.5f;
            return mid;
        }

        return Mathf.Clamp(targetZ, minZ, maxZ);
    }

    private void ResolveBlockByRigidbodies(Entity entity, Vector3 currentLogicPos, ref float targetX, ref float targetZ)
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

        if (!entityManager.IsBlockedByRigidbody(entity, targetX, targetZ))
        {
            return;
        }

        float xOnlyTarget = targetX;
        float xOnlyDepth = currentLogicPos.z;
        bool canMoveOnXOnly = !entityManager.IsBlockedByRigidbody(entity, xOnlyTarget, xOnlyDepth);
        if (canMoveOnXOnly)
        {
            targetZ = xOnlyDepth;
            return;
        }

        float zOnlyTarget = currentLogicPos.x;
        float zOnlyDepth = targetZ;
        bool canMoveOnZOnly = !entityManager.IsBlockedByRigidbody(entity, zOnlyTarget, zOnlyDepth);
        if (canMoveOnZOnly)
        {
            targetX = zOnlyTarget;
            return;
        }

        targetX = currentLogicPos.x;
        targetZ = currentLogicPos.z;
    }
}
