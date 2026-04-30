public sealed class AttackHitContext
{
    public DamageType DamageType { get; set; } = DamageType.Physical;
    public float DamageRatio { get; set; } = 1f;
    public bool IsFixedDamage { get; set; } = false;
    public float FixedDamageValue { get; set; } = 0f;
}
