public partial class Entity
{
    public bool IsDead { get; protected set; }
    public float CurrentHP { get; protected set; }
    public float CurrentSP { get; protected set; }
    /// <summary>
    /// /// 由索敌与判定模块在确认命中后调用：用双方 <see cref="Data"/> 结算伤害，并将结果交给受击方 <see cref="IAttackable.OnHit"/>。
    /// 统一攻击入口：可通过 <paramref name="hitContext"/> 透传技能/投掷物等来源信息。
    /// </summary>
    public bool TryAttackEntity(Entity defender, AttackHitContext hitContext)
    {
        if (defender == null || Data == null || defender.Data == null)
            return false;
        if (!(defender is IAttackable attackable))
            return false;

        DamageSettlementResult result = CombatDamageSettlement.Settle(this, defender, hitContext);
        attackable.OnHit(result);
        return true;
    }
}
