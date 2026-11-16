using cfg.combat.dragon;
using UnityEngine;

public static class CombatDamageSettlement
{
    /// <summary>
    /// 子弹命中。先按子弹上的命中 Buff 加到目标身上，再结算这一次伤害。
    /// </summary>
    public static bool TryApplyBulletHit(BulletBase bullet, Entity defender)
    {
        if (bullet == null || bullet.Config == null || bullet.Params == null || defender == null || defender.IsDead)
        {
            return false;
        }

        if (!(defender is IAttackable attackable))
        {
            return false;
        }

        Entity attacker = bullet.Snapshot != null ? bullet.Snapshot.Attacker : bullet.Owner;
        ApplyBuffs(bullet.Config.HitBuffIds, defender, attacker);
        ApplyBuffs(bullet.Config.HitBuffOnAttackerIds, attacker, attacker);

        float amount = Compute(
            bullet.Snapshot,
            defender,
            bullet.Config.DamageType == DamageType.Heal,
            bullet.Params.DamageRatio,
            bullet.Params.FlatDamage,
            bullet.Config.IsFixedDamage,
            bullet.Params.FixedDamage,
            bullet.Params.ExtraCritRate,
            bullet.Params.CritMultiplier,
            out bool isCritical);
        amount = ApplyResistance(defender, bullet.Config.DamageType, amount);

        DamageSettlementResult result = new DamageSettlementResult(attacker, amount, bullet.Config.DamageType, isCritical, bullet);
        attackable.OnHit(result);
        return true;
    }

    /// <summary>
    /// Buff 跳点。固定值不减防御；攻击力倍率走和子弹相同的攻防，治疗不减防御。两者都乘层数。
    /// </summary>
    public static void TryApplyBuffTick(BuffBase buff)
    {
        if (buff == null || buff.Config == null || buff.Host == null || buff.Host.IsDead)
        {
            return;
        }

        if (!(buff.Host is IAttackable attackable))
        {
            return;
        }

        float ratio = buff.Config.TickDamageRatio;
        float flat = buff.Config.TickDamage;
        if (ratio == 0f && flat == 0f)
        {
            return;
        }

        bool isHeal = buff.Config.TickDamageType == DamageType.Heal;
        Entity statSource = ResolveStatSource(buff.Caster);
        int stack = Mathf.Max(1, buff.Stack);
        float amount = ComputeBuffTick(statSource, buff.Host, isHeal, ratio, flat, stack);
        amount = ApplyResistance(buff.Host, buff.Config.TickDamageType, amount);
        if (amount <= 0f)
        {
            return;
        }

        DamageSettlementResult result = new DamageSettlementResult(statSource, amount, buff.Config.TickDamageType, false, null);
        attackable.OnHit(result);
    }

    private static void ApplyBuffs(int[] buffIds, Entity host, Entity caster)
    {
        if (buffIds == null || host == null)
        {
            return;
        }

        for (int i = 0; i < buffIds.Length; i++)
        {
            if (buffIds[i] > 0)
            {
                host.AddBuff(buffIds[i], caster);
            }
        }
    }

    private static float Compute(
        AttackerSnapshot snapshot,
        Entity defender,
        bool isHeal,
        float damageRatio,
        float flatDamage,
        bool isFixedDamage,
        float fixedDamage,
        float extraCritRate,
        float critMultiplierOverride,
        out bool isCritical)
    {
        isCritical = false;
        float takenRatio = 1f;
        if (defender != null && defender.CombatModifiers != null)
        {
            CombatParam takenParam = isHeal ? CombatParam.HealReceivedRatio : CombatParam.DamageTakenRatio;
            takenRatio = defender.CombatModifiers.Resolve(takenParam, 1f);
        }

        if (takenRatio < 0f)
        {
            takenRatio = 0f;
        }

        float raw;
        if (isFixedDamage)
        {
            raw = Mathf.Max(0f, fixedDamage);
        }
        else
        {
            float attack = snapshot != null ? snapshot.Attack : 0f;
            float dealtRatio = 1f;
            if (snapshot != null)
            {
                dealtRatio = isHeal ? snapshot.HealDealtRatio : snapshot.DamageDealtRatio;
            }

            if (dealtRatio < 0f)
            {
                dealtRatio = 0f;
            }

            raw = attack * Mathf.Max(0f, damageRatio) * dealtRatio + flatDamage;
            if (!isHeal)
            {
                raw -= GetDefence(defender);
            }

            if (raw < 0f)
            {
                raw = 0f;
            }
        }

        raw *= takenRatio;

        float critRate = snapshot != null ? snapshot.CritRate : 0f;
        critRate += extraCritRate;
        if (critRate > 0f && UnityEngine.Random.value < critRate)
        {
            float critMultiplier = critMultiplierOverride > 0f
                ? critMultiplierOverride
                : (snapshot != null ? snapshot.CritMultiplier : 1.5f);
            raw *= Mathf.Max(0f, critMultiplier);
            isCritical = true;
        }

        return Mathf.Max(0f, raw);
    }

    private static float ComputeBuffTick(Entity statSource, Entity defender, bool isHeal, float damageRatio, float flatDamage, int stack)
    {
        float takenRatio = 1f;
        if (defender != null && defender.CombatModifiers != null)
        {
            CombatParam takenParam = isHeal ? CombatParam.HealReceivedRatio : CombatParam.DamageTakenRatio;
            takenRatio = defender.CombatModifiers.Resolve(takenParam, 1f);
        }

        if (takenRatio < 0f)
        {
            takenRatio = 0f;
        }

        float attack = 0f;
        CharacterData data = statSource != null ? statSource.GetData<CharacterData>() : null;
        if (data != null)
        {
            attack = data.GetCurrentAttribute(cfg.entity.dragon.CharacterAttr.ATTACK);
        }

        float dealtRatio = 1f;
        if (statSource != null && statSource.CombatModifiers != null)
        {
            CombatParam dealtParam = isHeal ? CombatParam.HealDealtRatio : CombatParam.DamageDealtRatio;
            dealtRatio = statSource.CombatModifiers.Resolve(dealtParam, 1f);
        }

        if (dealtRatio < 0f)
        {
            dealtRatio = 0f;
        }

        float fromAttack = attack * Mathf.Max(0f, damageRatio) * dealtRatio;
        if (!isHeal)
        {
            fromAttack -= GetDefence(defender);
        }

        if (fromAttack < 0f)
        {
            fromAttack = 0f;
        }

        float flat = Mathf.Max(0f, flatDamage);
        return Mathf.Max(0f, (fromAttack + flat) * stack * takenRatio);
    }

    private static float ApplyResistance(Entity defender, DamageType damageType, float amount)
    {
        if (defender == null || amount <= 0f)
        {
            return amount;
        }

        float resistance = defender.GetResistance(damageType);
        return amount * (1f - resistance);
    }

    private static Entity ResolveStatSource(Entity caster)
    {
        if (caster is CombatProp prop && prop.Source != null)
        {
            return prop.Source;
        }

        return caster;
    }

    private static float GetDefence(Entity defender)
    {
        CharacterData data = defender != null ? defender.GetData<CharacterData>() : null;
        if (data == null)
        {
            return 0f;
        }

        return data.GetCurrentAttribute(cfg.entity.dragon.CharacterAttr.DEFENCE);
    }
}
