using System.Collections.Generic;
using cfg.combat.dragon;

/// <summary>
/// 一个阵营在本局战斗里的修改器来源。肉鸽奖励记在这里，再推到当前还活着的成员身上。
/// 已经飞出去的子弹不回溯，只在下一次生成时烘焙 Bullet 范围的修改。
/// </summary>
public sealed class FactionCombatContext
{
    private sealed class Grant
    {
        public int Id;
        public CombatModifierSpec Spec;
        public readonly Dictionary<int, ModifierLease> Leases = new Dictionary<int, ModifierLease>();
    }

    private readonly List<Entity> _entities = new List<Entity>();
    private readonly List<EmitterBase> _emitters = new List<EmitterBase>();
    private readonly List<Grant> _grants = new List<Grant>();
    private int _nextGrantId = 1;

    public int FactionId { get; }

    public FactionCombatContext(int factionId)
    {
        FactionId = factionId;
    }

    public void AttachEntity(Entity entity)
    {
        if (entity == null || _entities.Contains(entity))
        {
            return;
        }

        _entities.Add(entity);
        for (int i = 0; i < _grants.Count; i++)
        {
            ApplyEntityGrant(_grants[i], entity);
        }
    }

    public void DetachEntity(Entity entity)
    {
        if (entity == null)
        {
            return;
        }

        _entities.Remove(entity);
        for (int i = 0; i < _grants.Count; i++)
        {
            ReleaseLease(_grants[i], entity.InstanceId);
        }
    }

    public void AttachEmitter(EmitterBase emitter)
    {
        if (emitter == null || _emitters.Contains(emitter))
        {
            return;
        }

        _emitters.Add(emitter);
        for (int i = 0; i < _grants.Count; i++)
        {
            ApplyEmitterGrant(_grants[i], emitter);
        }
    }

    public void DetachEmitter(EmitterBase emitter)
    {
        if (emitter == null)
        {
            return;
        }

        _emitters.Remove(emitter);
        for (int i = 0; i < _grants.Count; i++)
        {
            ReleaseLease(_grants[i], emitter.InstanceId);
        }
    }

    public int AddGrant(CombatModifierSpec spec)
    {
        if (spec == null)
        {
            return 0;
        }

        Grant grant = new Grant
        {
            Id = _nextGrantId++,
            Spec = spec.Clone(),
        };
        grant.Spec.SourceId = grant.Id;
        _grants.Add(grant);

        if (grant.Spec.Scope == CombatModifierScope.Entity)
        {
            for (int i = 0; i < _entities.Count; i++)
            {
                ApplyEntityGrant(grant, _entities[i]);
            }
        }
        else if (grant.Spec.Scope == CombatModifierScope.Emitter)
        {
            for (int i = 0; i < _emitters.Count; i++)
            {
                ApplyEmitterGrant(grant, _emitters[i]);
            }
        }

        return grant.Id;
    }

    public bool RemoveGrant(int grantId)
    {
        for (int i = 0; i < _grants.Count; i++)
        {
            if (_grants[i].Id != grantId)
            {
                continue;
            }

            ReleaseAll(_grants[i]);
            _grants.RemoveAt(i);
            return true;
        }

        return false;
    }

    public float ResolveBulletParam(CombatParam param, float baseValue)
    {
        float flat = 0f;
        float percent = 0f;
        for (int i = 0; i < _grants.Count; i++)
        {
            CombatModifierSpec spec = _grants[i].Spec;
            if (spec.Scope != CombatModifierScope.Bullet || spec.Param != param)
            {
                continue;
            }

            if (spec.Op == CombatModifierOp.Flat)
            {
                flat += spec.Value;
            }
            else
            {
                percent += spec.Value;
            }
        }

        return CombatModifierMath.Apply(baseValue, flat, percent);
    }

    public bool HasBulletParam(CombatParam param)
    {
        for (int i = 0; i < _grants.Count; i++)
        {
            CombatModifierSpec spec = _grants[i].Spec;
            if (spec.Scope == CombatModifierScope.Bullet && spec.Param == param)
            {
                return true;
            }
        }

        return false;
    }

    public void AccumulateBullet(CombatParam param, ref float flat, ref float percent)
    {
        for (int i = 0; i < _grants.Count; i++)
        {
            CombatModifierSpec spec = _grants[i].Spec;
            if (spec.Scope != CombatModifierScope.Bullet || spec.Param != param || spec.UseCharacterAttr)
            {
                continue;
            }

            if (spec.Op == CombatModifierOp.Flat)
            {
                flat += spec.Value;
            }
            else
            {
                percent += spec.Value;
            }
        }
    }

    public void Clear()
    {
        for (int i = 0; i < _grants.Count; i++)
        {
            ReleaseAll(_grants[i]);
        }

        _grants.Clear();
        _entities.Clear();
        _emitters.Clear();
    }

    private static void ApplyEntityGrant(Grant grant, Entity entity)
    {
        if (grant.Spec.Scope != CombatModifierScope.Entity || entity == null)
        {
            return;
        }

        if (grant.Leases.ContainsKey(entity.InstanceId))
        {
            return;
        }

        ModifierLease lease = new ModifierLease();
        CombatModifierRouter.ApplyToEntity(entity, grant.Spec, lease);
        grant.Leases[entity.InstanceId] = lease;
    }

    private static void ApplyEmitterGrant(Grant grant, EmitterBase emitter)
    {
        if (grant.Spec.Scope != CombatModifierScope.Emitter || emitter == null)
        {
            return;
        }

        if (grant.Leases.ContainsKey(emitter.InstanceId))
        {
            return;
        }

        ModifierLease lease = new ModifierLease();
        CombatModifierRouter.ApplyToEmitter(emitter, grant.Spec, lease);
        grant.Leases[emitter.InstanceId] = lease;
    }

    private static void ReleaseLease(Grant grant, int targetId)
    {
        if (!grant.Leases.TryGetValue(targetId, out ModifierLease lease))
        {
            return;
        }

        lease.Release();
        grant.Leases.Remove(targetId);
    }

    private static void ReleaseAll(Grant grant)
    {
        foreach (KeyValuePair<int, ModifierLease> pair in grant.Leases)
        {
            pair.Value.Release();
        }

        grant.Leases.Clear();
    }
}
