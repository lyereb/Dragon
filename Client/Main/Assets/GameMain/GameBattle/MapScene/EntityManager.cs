using System.Collections.Generic;
using UnityEngine;

public class EntityManager
{
    private readonly List<Entity> _entities = new List<Entity>();
    private readonly Dictionary<Entity, GameObject> _entitySkinMap = new Dictionary<Entity, GameObject>();
    private readonly Dictionary<EntityId, Vector3> _bodyRestOffset = new Dictionary<EntityId, Vector3>();
    private Transform _entityRoot;

    public IReadOnlyList<Entity> Entities => _entities;

    public void Initialize()
    {
        _entities.Clear();
        _entitySkinMap.Clear();
        _bodyRestOffset.Clear();
        InitializeEntityRoot();
    }

    public void AddEntity(Entity entity)
    {
        if (entity == null)
        {
            return;
        }

        _entities.Add(entity);
        RegisterEntitySkin(entity);
    }

    public void AddEntities(IEnumerable<Entity> entities)
    {
        if (entities == null)
        {
            return;
        }

        foreach (Entity entity in entities)
        {
            AddEntity(entity);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < _entities.Count; i++)
        {
            Entity entity = _entities[i];
            if (entity == null)
            {
                continue;
            }

            entity.Uninitialize();
            UnregisterAndDestroyEntitySkin(entity);
        }

        _entities.Clear();
        _entitySkinMap.Clear();
        _bodyRestOffset.Clear();
    }

    public void Update(float dt)
    {
        for (int i = 0; i < _entities.Count; i++)
        {
            Entity entity = _entities[i];
            if (entity == null || entity.IsDespawnPending)
            {
                continue;
            }

            entity.Update(dt);
            SyncEntitySkinTransform(entity);
        }

        RemoveDespawned();
    }

    public void BindSkin(Entity entity)
    {
        RegisterEntitySkin(entity);
    }

    public void LateUpdate(float dt)
    {
        for (int i = 0; i < _entities.Count; i++)
        {
            _entities[i].LateUpdate(dt);
        }
    }

    //占位检测, entity是否被其他entity的rigidbody阻挡
    //mover: 移动者
    //targetX: 目标X（水平）
    //targetY: 目标Y（深度）
    //返回值: 是否被阻挡
    public bool IsBlockedByRigidbody(Entity mover, float targetX, float targetY)
    {
        if (mover == null || mover.Data == null)
        {
            return false;
        }

        float moverHalfWidth = Mathf.Max(0f, mover.Data.WidthSize * 0.5f);
        float moverHalfDepth = Mathf.Max(0f, mover.Data.DepthSize * 0.5f);
        float moverMinX = targetX - moverHalfWidth;
        float moverMaxX = targetX + moverHalfWidth;
        float moverMinY = targetY - moverHalfDepth;
        float moverMaxY = targetY + moverHalfDepth;

        for (int i = 0; i < _entities.Count; i++)
        {
            Entity other = _entities[i];
            if (other == null || other == mover || other.Data == null || other.Data.Position == null)
            {
                continue;
            }

            if (!other.Data.isRigidbody)
            {
                continue;
            }

            Vector3 otherPos = other.Data.Position.LogicPosition;
            float otherHalfWidth = Mathf.Max(0f, other.Data.WidthSize * 0.5f);
            float otherHalfDepth = Mathf.Max(0f, other.Data.DepthSize * 0.5f);
            float otherMinX = otherPos.x - otherHalfWidth;
            float otherMaxX = otherPos.x + otherHalfWidth;
            float otherMinY = otherPos.y - otherHalfDepth;
            float otherMaxY = otherPos.y + otherHalfDepth;

            bool overlapX = moverMinX < otherMaxX && moverMaxX > otherMinX;
            bool overlapY = moverMinY < otherMaxY && moverMaxY > otherMinY;
            if (overlapX && overlapY)
            {
                return true;
            }
        }

        return false;
    }

    private void InitializeEntityRoot()
    {
        GameObject entityRootObject = GameObject.Find("EntityRoot");
        if (entityRootObject == null)
        {
            entityRootObject = new GameObject("EntityRoot");
        }
        _entityRoot = entityRootObject.transform;

        EntityOccupancyGizmoDrawer gizmoDrawer = entityRootObject.GetComponent<EntityOccupancyGizmoDrawer>();
        if (gizmoDrawer == null)
        {
            gizmoDrawer = entityRootObject.AddComponent<EntityOccupancyGizmoDrawer>();
        }
        gizmoDrawer.Bind(this);
    }

    private void RemoveDespawned()
    {
        for (int i = _entities.Count - 1; i >= 0; i--)
        {
            Entity entity = _entities[i];
            if (entity == null || !entity.IsDespawnPending)
            {
                continue;
            }

            entity.Uninitialize();
            UnregisterAndDestroyEntitySkin(entity);
            _entities.RemoveAt(i);
        }
    }

    private void RegisterEntitySkin(Entity entity)
    {
        if (entity == null || entity.Skin == null)
        {
            return;
        }

        if (_entityRoot == null)
        {
            InitializeEntityRoot();
        }

        entity.Skin.transform.SetParent(_entityRoot, false);
        SyncEntitySkinTransform(entity);
        _entitySkinMap[entity] = entity.Skin;
    }

    private void SyncEntitySkinTransform(Entity entity)
    {
        if (entity == null || entity.Data?.Position == null)
        {
            return;
        }

        if (!_entitySkinMap.TryGetValue(entity, out GameObject skin) || skin == null)
        {
            return;
        }

        skin.transform.position = entity.Data.Position.GetRenderPosition();
        Transform shadow = skin.transform.Find("Shadow");
        if (shadow != null)
        {
            SetLocalOffset(shadow, Vector3.zero);
        }

        Transform body = skin.transform.Find("Body");
        if (body == null)
        {
            return;
        }

        EntityId bodyId = body.GetEntityId();
        if (!_bodyRestOffset.TryGetValue(bodyId, out Vector3 restOffset))
        {
            restOffset = GetLocalOffset(body);
            _bodyRestOffset.Add(bodyId, restOffset);
        }

        restOffset.y += entity.Data.Position.Z * GlobalDefine.JumpVisualLift;
        SetLocalOffset(body, restOffset);
    }

    private static Vector3 GetLocalOffset(Transform node)
    {
        RectTransform rect = node as RectTransform;
        if (rect != null)
        {
            return new Vector3(rect.anchoredPosition.x, rect.anchoredPosition.y, node.localPosition.z);
        }

        return node.localPosition;
    }

    private static void SetLocalOffset(Transform node, Vector3 offset)
    {
        RectTransform rect = node as RectTransform;
        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(offset.x, offset.y);
            return;
        }

        node.localPosition = offset;
    }

    private void UnregisterAndDestroyEntitySkin(Entity entity)
    {
        if (entity == null)
        {
            return;
        }

        if (_entitySkinMap.TryGetValue(entity, out GameObject skin))
        {
            if (skin != null)
            {
                Transform body = skin.transform.Find("Body");
                if (body != null)
                {
                    _bodyRestOffset.Remove(body.GetEntityId());
                }

                Object.Destroy(skin);
            }
            _entitySkinMap.Remove(entity);
            return;
        }

        if (entity.Skin != null)
        {
            Object.Destroy(entity.Skin);
        }
    }
}