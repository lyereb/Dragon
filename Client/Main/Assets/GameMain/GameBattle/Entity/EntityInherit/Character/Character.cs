using UnityEngine;

public class Character : Entity, IAttackable
{
    public new CharacterData Data => GetData<CharacterData>();

    protected override EntityData CreateEntityData(EntityInitContext context)
    {
        CharacterData data = new CharacterData();
        ApplyEntityData(data, context);
        return data;
    }

    protected override void ApplyEntityData(EntityData data, EntityInitContext context)
    {
        base.ApplyEntityData(data, context);
        if (data is CharacterData characterData && context is CharacterInitContext characterContext)
        {
            ApplyCharacterData(characterData, characterContext);
            ApplyUserSaveData(characterData, characterContext);
        }
    }

    protected virtual void ApplyCharacterData(CharacterData data, CharacterInitContext context)
    {

    }

    /// <summary>
    /// 预留：在配置属性应用完成后注入用户存档加成（如等级、养成、被动等）。
    /// </summary>
    protected virtual void ApplyUserSaveData(CharacterData data, CharacterInitContext context)
    {
    }

    protected override EntityType GetEntityType()
    {
        return EntityType.Character;
    }

    protected virtual CharacterType GetCharacterType()
    {
        return default;
    }

    protected override bool OnFSMSetup()
    {
        FSM.Register(new CharacterBirthState());
        FSM.Register(new CharacterIdleState());
        FSM.Register(new CharacterMoveState());
        FSM.Register(new CharacterRunState());
        FSM.Register(new CharacterJumpState());
        FSM.Register(new CharacterDeathState());
        return true;
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

    public virtual void OnHit(DamageSettlementResult result)
    {
        if (result == null)
            return;

        // 扣除当前生命值（不做 clamp，按存档/公式结果原样反映）
        CurrentHP -= result.FinalDamage;

        if (CurrentHP <= 0f)
        {
            CurrentHP = 0f;
            IsDead = true;
            // 进入死亡状态（由 FSM 驱动后续死亡流程）
            FSM.TransToState((int)EntityStateCode.Death, null);
        }
    }
}