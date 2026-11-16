using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 命中后从被打中的目标往外找下一跳。
/// chainInstant 为真时立刻出现在下一目标身上，否则沿直线飞过去。
/// 第一击使用烤好的伤害倍率，之后每一跳乘一次 chainDamageDecay。
/// </summary>
public class ChainBullet : BulletBase
{
    private readonly List<Entity> _candidates = new List<Entity>();
    private float _baseDamageRatio = 1f;
    private int _hopsDone;
    private bool _continue;
    private bool _pendingJump;

    protected override void OnBirth()
    {
        if (Params != null)
        {
            _baseDamageRatio = Params.DamageRatio;
        }
    }

    protected override void OnTickBirth(float dt)
    {
        if (_pendingJump)
        {
            _pendingJump = false;
            if (GatherHits() > 0)
            {
                ChangeToState(BulletStateCode.Hit);
                return;
            }

            ChangeToState(BulletStateCode.Death);
            return;
        }

        Move(dt, out _);
        bool expired = IsExpired();
        bool outOfRange = IsOutOfRange();
        if (GatherHits() > 0)
        {
            ChangeToState(BulletStateCode.Hit);
            return;
        }

        if (outOfRange || expired)
        {
            ChangeToState(BulletStateCode.Death);
        }
    }

    protected override void OnHitsApplied(List<Entity> targets)
    {
        _continue = false;
        if (targets == null || targets.Count == 0 || Config == null || Params == null)
        {
            return;
        }

        int chainCount = Config.ChainCount < 0 ? 0 : Config.ChainCount;
        _hopsDone++;
        if (_hopsDone > chainCount)
        {
            return;
        }

        Entity next = FindNext(targets[0]);
        if (next == null || next.Data == null || next.Data.Position == null)
        {
            return;
        }

        float decay = Config.ChainDamageDecay < 0f ? 0f : Config.ChainDamageDecay;
        Params.DamageRatio = _baseDamageRatio * Mathf.Pow(decay, _hopsDone);
        PreferredTarget = next;
        Vector3 nextPosition = next.Data.Position.LogicPosition;
        if (Config.ChainInstant || CombatGeometry.InRange(Position, next, Params.HitRadius))
        {
            Position = nextPosition;
            _pendingJump = true;
        }
        else
        {
            Direction = CombatGeometry.FlattenXY(nextPosition - Position);
        }

        _continue = true;
    }

    protected override bool ShouldDespawnAfterHit()
    {
        return !_continue;
    }

    private Entity FindNext(Entity anchor)
    {
        if (anchor == null || anchor.Data == null || anchor.Data.Position == null || Battle == null || Config == null)
        {
            return null;
        }

        float radius = Config.ChainRadius < 0f ? 0f : Config.ChainRadius;
        Vector3 origin = anchor.Data.Position.LogicPosition;
        Battle.CollectHittable(_candidates);
        Entity nearest = null;
        float best = float.MaxValue;
        for (int i = 0; i < _candidates.Count; i++)
        {
            Entity candidate = _candidates[i];
            if (candidate == null || candidate.Data == null || candidate.Data.Position == null)
            {
                continue;
            }

            if (!CanHit(candidate) || !CombatGeometry.InRange(origin, candidate, radius))
            {
                continue;
            }

            float distance = CombatGeometry.DistanceXY(origin, candidate.Data.Position.LogicPosition);
            if (distance < best)
            {
                best = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }
}
