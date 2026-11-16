using cfg.combat.dragon;
using cfg.entity.dragon;

/// <summary>
/// 把一条修改器落到它该待的对象上，并把撤销动作记进 lease。
/// 角色属性进 CharacterData；其它战斗参数进对象自己的修改板。
/// </summary>
public static class CombatModifierRouter
{
    public static void ApplyToEntity(Entity entity, CombatModifierSpec spec, ModifierLease lease)
    {
        if (entity == null || spec == null || lease == null || spec.Scope != CombatModifierScope.Entity)
        {
            return;
        }

        if (spec.UseCharacterAttr)
        {
            CharacterData data = entity.GetData<CharacterData>();
            if (data == null)
            {
                return;
            }

            CharacterAttrModifierKind kind = spec.Op == CombatModifierOp.Percent
                ? CharacterAttrModifierKind.Percent
                : CharacterAttrModifierKind.Flat;
            int modifierId = data.AddModifier(spec.CharacterAttr, kind, spec.Value);
            lease.Track(() => data.RemoveModifier(modifierId));
            return;
        }

        int handle = entity.CombatModifiers.Add(spec);
        if (handle > 0)
        {
            lease.Track(() => entity.CombatModifiers.Remove(handle));
        }
    }

    public static void ApplyToEmitter(EmitterBase emitter, CombatModifierSpec spec, ModifierLease lease)
    {
        if (emitter == null || spec == null || lease == null)
        {
            return;
        }

        if (spec.Scope != CombatModifierScope.Emitter && spec.Scope != CombatModifierScope.Bullet)
        {
            return;
        }

        int handle = emitter.CombatModifiers.Add(spec);
        if (handle > 0)
        {
            lease.Track(() => emitter.CombatModifiers.Remove(handle));
        }
    }
}
