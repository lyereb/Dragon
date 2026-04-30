using UnityEngine;
using Dragon.Config;
using Dragon.GameInput;
using cfg.entity.dragon;
public class Player : Character
{
    public new PlayerData Data => GetData<PlayerData>();

    protected override EntityData CreateEntityData(EntityInitContext context)
    {
        PlayerData data = new PlayerData();
        ApplyEntityData(data, context);
        return data;
    }

    protected override void ApplyCharacterData(CharacterData data, CharacterInitContext context)
    {
        base.ApplyCharacterData(data, context);
        if (data is PlayerData playerData && context is PlayerInitContext playerContext)
        {
            ApplyPlayerData(playerData, playerContext);
        }
    }

    protected virtual void ApplyPlayerData(PlayerData data, PlayerInitContext context)
    {
        player config = ConfigManager.Instance.Tables.Tbplayer.GetOrDefault(context.EntityConfigId);
        if (config == null)
        {
            Debug.LogError($"Player.ApplyPlayerData fail, id={context.EntityConfigId}");
            return;
        }
        data.Config = config;
        data.WidthSize = config.Width;
        data.DepthSize = config.Depth;
        data.isRigidbody = config.IsRigidBody;
        data.ApplyAttributesFromConfig(config.Attribute);

        //初始化战斗状态值(当前生命值等等)
        CurrentHP = data.GetCurrentAttribute(CharacterAttr.MAX_HP);
        CurrentSP = data.GetCurrentAttribute(CharacterAttr.MAX_SP);
    }

    protected override void ApplyUserSaveData(CharacterData data, CharacterInitContext context)
    {
        base.ApplyUserSaveData(data, context);
        if (data is PlayerData playerData && context is PlayerInitContext playerContext)
        {
            ApplyUserSaveData(playerData, playerContext);
        }
    }

    protected virtual void ApplyUserSaveData(PlayerData data, PlayerInitContext context)
    {
        //TODO: 应用用户存档数据
    }

    protected override string GetSkinPrefabName(EntityInitContext context)
    {
        player config = ConfigManager.Instance.Tables.Tbplayer.GetOrDefault(context.EntityConfigId);
        if (config == null)
        {
            Debug.LogError($"Player.GetSkinPrefabName fail, id={context.EntityConfigId}");
            return null;
        }
        return config.PrefabName;
    }
    protected override CharacterType GetCharacterType()
    {
        return CharacterType.Player;
    }

    protected override IInputProvider CreateInputProvider()
    {
        var inputProvider = new PlayerInputProvider();
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
