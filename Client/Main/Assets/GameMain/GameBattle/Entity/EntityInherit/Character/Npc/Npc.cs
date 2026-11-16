using UnityEngine;
using Dragon.Config;
using cfg.entity.dragon;

public class Npc : Character
{
    public new NpcData Data => GetData<NpcData>();

    protected override EntityData CreateEntityData(EntityInitContext context)
    {
        NpcData data = new NpcData();
        ApplyEntityData(data, context);
        return data;
    }

    protected override void ApplyCharacterData(CharacterData data, CharacterInitContext context)
    {
        base.ApplyCharacterData(data, context);
        if (data is NpcData npcData && context is NpcInitContext npcContext)
        {
            ApplyNpcData(npcData, npcContext);
        }
    }

    protected virtual void ApplyNpcData(NpcData data, NpcInitContext context)
    {
        npc config = ConfigManager.Instance.Tables.Tbnpc.GetOrDefault(context.EntityConfigId);
        if (config == null)
        {
            Debug.LogError($"Npc.ApplyNpcData fail, id={context.EntityConfigId}");
            return;
        }
        data.Config = config;
        data.WidthSize = config.Width;
        data.DepthSize = config.Depth;
        data.isRigidbody = config.IsRigidBody;
        data.ApplyAttributesFromConfig(config.Attribute);
        Resistances.SetBase(config.Resistance);

        //初始化战斗状态值(当前生命值等等)
        CurrentHP = data.GetCurrentAttribute(CharacterAttr.MAX_HP);
        CurrentSP = data.GetCurrentAttribute(CharacterAttr.MAX_SP);
    }

    protected override string GetSkinPrefabName(EntityInitContext context)
    {
        npc config = ConfigManager.Instance.Tables.Tbnpc.GetOrDefault(context.EntityConfigId);
        if (config == null)
        {
            Debug.LogError($"Npc.GetSkinPrefabName fail, id={context.EntityConfigId}");
            return null;
        }
        return config.PrefabName;
    }

    protected override CharacterType GetCharacterType()
    {
        return CharacterType.NPC;
    }

    protected override IInputProvider CreateInputProvider()
    {
        var inputProvider = new AIInputProvider();
        inputProvider.Init();
        return inputProvider;
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
