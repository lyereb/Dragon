using System.Collections.Generic;
using cfg.combat.dragon;
using UnityEngine;

/// <summary>
/// 索敌器基类。发射器持有一个，按表格里的类型和范围挑目标。
/// 主目标用于瞄准；Collect 把符合条件的目标按距离从近到远放进结果。
/// </summary>
public abstract class TargetQueryBase
{
    private Entity _lockedTarget;
    private float _lockRemain;

    protected EmitterBase Emitter { get; private set; }

    public void Bind(EmitterBase emitter)
    {
        Emitter = emitter;
    }

    public void Update(float dt)
    {
        if (_lockRemain > 0f)
        {
            _lockRemain -= dt;
        }
    }

    public Entity GetPrimaryTarget()
    {
        if (IsLockValid())
        {
            return _lockedTarget;
        }

        List<Entity> buffer = new List<Entity>();
        Collect(buffer);
        _lockedTarget = buffer.Count > 0 ? buffer[0] : null;
        _lockRemain = Emitter.Config.TargetLockDuration;
        return _lockedTarget;
    }

    public void Collect(List<Entity> results)
    {
        results.Clear();
        if (Emitter == null || Emitter.Owner == null || Emitter.Battle == null)
        {
            return;
        }

        Vector3 origin = Emitter.Owner.Data != null && Emitter.Owner.Data.Position != null
            ? Emitter.Owner.Data.Position.LogicPosition
            : Vector3.zero;
        float radius = Emitter.Resolve(CombatParam.SearchRadius, Emitter.Config.SearchRadius);
        int maxCount = Mathf.Max(1, Emitter.Config.MaxTargetCount);

        List<Entity> candidates = CollectCandidates(origin, radius);
        SortByDistance(origin, candidates);
        int count = Mathf.Min(maxCount, candidates.Count);
        for (int i = 0; i < count; i++)
        {
            results.Add(candidates[i]);
        }
    }

    public void ClearLock()
    {
        _lockedTarget = null;
        _lockRemain = 0f;
    }

    protected abstract List<Entity> CollectCandidates(Vector3 origin, float radius);

    protected List<Entity> QueryInRadius(Vector3 origin, float radius)
    {
        List<Entity> found = new List<Entity>();
        List<Entity> all = new List<Entity>();
        Emitter.Battle.CollectHittable(all);
        for (int i = 0; i < all.Count; i++)
        {
            Entity candidate = all[i];
            if (!CombatTargetUtility.PassFilter(Emitter.Owner, candidate, Emitter.Config.TargetRelation))
            {
                continue;
            }

            if (!CombatGeometry.InRange(origin, candidate, radius))
            {
                continue;
            }

            found.Add(candidate);
        }

        return found;
    }

    private bool IsLockValid()
    {
        if (_lockedTarget == null || Emitter == null || Emitter.Config == null || Emitter.Owner == null || Emitter.Owner.Data == null || Emitter.Owner.Data.Position == null)
        {
            return false;
        }

        if (Emitter.Config.TargetLockDuration <= 0f || _lockRemain <= 0f)
        {
            return false;
        }

        if (!CombatTargetUtility.PassFilter(Emitter.Owner, _lockedTarget, Emitter.Config.TargetRelation))
        {
            return false;
        }

        Vector3 origin = Emitter.Owner.Data.Position.LogicPosition;
        float radius = Emitter.Resolve(CombatParam.SearchRadius, Emitter.Config.SearchRadius);
        return CombatGeometry.InRange(origin, _lockedTarget, radius);
    }

    private static void SortByDistance(Vector3 origin, List<Entity> entities)
    {
        for (int i = 1; i < entities.Count; i++)
        {
            Entity current = entities[i];
            float currentDistance = CombatGeometry.DistanceXY(origin, current.Data.Position.LogicPosition);
            int j = i - 1;
            while (j >= 0)
            {
                float otherDistance = CombatGeometry.DistanceXY(origin, entities[j].Data.Position.LogicPosition);
                if (otherDistance <= currentDistance)
                {
                    break;
                }

                entities[j + 1] = entities[j];
                j--;
            }

            entities[j + 1] = current;
        }
    }
}

/// <summary>
/// 只关心最近目标。Collect 的结果已经按距离截断。
/// </summary>
public class NearestTargetQuery : TargetQueryBase
{
    protected override List<Entity> CollectCandidates(Vector3 origin, float radius)
    {
        return QueryInRadius(origin, radius);
    }
}

/// <summary>
/// 范围内全部目标。和最近目标的差别留给以后的多目标发射器，主目标规则相同。
/// </summary>
public class AllInRangeTargetQuery : TargetQueryBase
{
    protected override List<Entity> CollectCandidates(Vector3 origin, float radius)
    {
        return QueryInRadius(origin, radius);
    }
}

public static class TargetQueryFactory
{
    public static TargetQueryBase Create(TargetQueryType queryType)
    {
        switch (queryType)
        {
            case TargetQueryType.Nearest:
                return new NearestTargetQuery();
            case TargetQueryType.AllInRange:
                return new AllInRangeTargetQuery();
            default:
                Debug.LogError($"TargetQueryFactory.Create fail, unknown type={queryType}");
                return new NearestTargetQuery();
        }
    }
}
