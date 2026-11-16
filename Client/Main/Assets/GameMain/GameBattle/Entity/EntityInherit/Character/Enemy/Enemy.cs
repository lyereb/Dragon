using UnityEngine;
using Dragon.Config;
using cfg.entity.dragon;

public class Enemy : Character
{
    public new EnemyData Data => GetData<EnemyData>();

    protected override EntityData CreateEntityData(EntityInitContext context)
    {
        EnemyData data = new EnemyData();
        ApplyEntityData(data, context);
        return data;
    }

    protected override void ApplyCharacterData(CharacterData data, CharacterInitContext context)
    {
        base.ApplyCharacterData(data, context);
        if (data is EnemyData enemyData && context is EnemyInitContext enemyContext)
        {
            ApplyEnemyData(enemyData, enemyContext);
        }
    }

    protected virtual void ApplyEnemyData(EnemyData data, EnemyInitContext context)
    {
        enemy config = ConfigManager.Instance.Tables.Tbenemy.GetOrDefault(context.EntityConfigId);
        if (config == null)
        {
            Debug.LogError($"Enemy.ApplyEnemyData fail, id={context.EntityConfigId}");
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
        enemy config = ConfigManager.Instance.Tables.Tbenemy.GetOrDefault(context.EntityConfigId);
        if (config == null)
        {
            Debug.LogError($"Enemy.GetSkinPrefabName fail, id={context.EntityConfigId}");
            return null;
        }
        return config.PrefabName;
    }

    protected override CharacterType GetCharacterType()
    {
        return CharacterType.Enemy;
    }

    protected override IInputProvider CreateInputProvider()
    {
        var inputProvider = new AIInputProvider();
        inputProvider.Init();
        return inputProvider;
    }

    protected override int GetDefaultFactionId()
    {
        return CombatFaction.Enemy;
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
