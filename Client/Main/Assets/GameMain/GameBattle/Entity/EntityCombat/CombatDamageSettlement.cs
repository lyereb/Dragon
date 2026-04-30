using System;
using cfg.entity.dragon;

public static class CombatDamageSettlement
{
    /// <summary>
    /// 统一伤害结算入口，使用双方 <see cref="Entity.Data"/>（运行时多为各实体子类对应的 <c>*Data</c>）。
    /// </summary>
    public static DamageSettlementResult Settle(Entity attacker, Entity defender, AttackHitContext hitContext)
    {
        if (attacker == null)
            throw new ArgumentNullException(nameof(attacker));
        if (defender == null)
            throw new ArgumentNullException(nameof(defender));
        if (hitContext == null)
            hitContext = new AttackHitContext();

        if (attacker.Data == null)
            throw new ArgumentException("attacker.Data is null.", nameof(attacker));
        if (defender.Data == null)
            throw new ArgumentException("defender.Data is null.", nameof(defender));

        float finalDamage = ComputeFinalDamage(attacker, defender);

        return new DamageSettlementResult(attacker, finalDamage, hitContext.DamageType, false);
    }

    static float ComputeFinalDamage(Entity attacker, Entity defender)
    {
        CharacterData atkChar = attacker.GetData<CharacterData>();
        CharacterData defChar = defender.GetData<CharacterData>();
        if (atkChar != null && defChar != null)
        {
            float attack = atkChar.GetCurrentAttribute(CharacterAttr.ATTACK);
            float defence = defChar.GetCurrentAttribute(CharacterAttr.DEFENCE);
            return Math.Max(0f, attack - defence);
        }

        // TODO: 其他 Entity 子类组合（如 InteractableData 受击）
        return 0f;
    }
}
