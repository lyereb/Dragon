public sealed class DamageSettlementResult
{
    public Entity Attacker { get; }
    public float FinalDamage { get; }
    public DamageType DamageType { get; }
    public bool IsCritical { get; }

    internal DamageSettlementResult(Entity attacker, float finalDamage, DamageType damageType, bool isCritical)
    {
        Attacker = attacker;
        FinalDamage = finalDamage;
        DamageType = damageType;
        IsCritical = isCritical;
    }
}
