using cfg.combat.dragon;

public sealed class DamageSettlementResult
{
    public Entity Attacker { get; }
    public float FinalDamage { get; }
    public DamageType DamageType { get; }
    public bool IsCritical { get; }
    public BulletBase SourceBullet { get; }

    internal DamageSettlementResult(Entity attacker, float finalDamage, DamageType damageType, bool isCritical, BulletBase sourceBullet)
    {
        Attacker = attacker;
        FinalDamage = finalDamage;
        DamageType = damageType;
        IsCritical = isCritical;
        SourceBullet = sourceBullet;
    }
}
