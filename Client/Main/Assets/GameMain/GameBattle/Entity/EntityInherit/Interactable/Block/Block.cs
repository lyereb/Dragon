using UnityEngine;
using Dragon.Config;
using cfg.entity.dragon;

public class Block : Interactable
{
    public new BlockData Data => GetData<BlockData>();

    protected override EntityData CreateEntityData(EntityInitContext context)
    {
        BlockData data = new BlockData();
        ApplyEntityData(data, context);
        return data;
    }

    protected override void ApplyInteractableData(InteractableData data, InteractableInitContext context)
    {
        base.ApplyInteractableData(data, context);
        if (data is BlockData blockData && context is BlockInitContext blockContext)
        {
            ApplyBlockData(blockData, blockContext);
        }
    }

    protected virtual void ApplyBlockData(BlockData data, BlockInitContext context)
    {
        block config = ConfigManager.Instance.Tables.Tbblock.GetOrDefault(context.EntityConfigId);
        if (config == null)
        {
            Debug.LogError($"Block.ApplyBlockData fail, id={context.EntityConfigId}");
            return;
        }
        data.Config = config;
        data.WidthSize = config.Width;
        data.DepthSize = config.Depth;
        data.isRigidbody = config.IsRigidBody;
    }

    protected override string GetSkinPrefabName(EntityInitContext context)
    {
        block config = ConfigManager.Instance.Tables.Tbblock.GetOrDefault(context.EntityConfigId);
        if (config == null)
        {
            Debug.LogError($"Block.GetSkinPrefabName fail, id={context.EntityConfigId}");
            return null;
        }
        return config.PrefabName;
    }

    protected override InteractableType GetInteractableType()
    {
        return InteractableType.Block;
    }

    protected override bool OnInitAfter()
    {
        return true;
    }

    protected override void OnUpdate(float dt)
    {
        base.OnUpdate(dt);
    }

    protected override void OnLateUpdate(float dt)
    {
        base.OnLateUpdate(dt);
    }
}
