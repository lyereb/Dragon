using System.Collections.Generic;
using cfg.combat.dragon;
using UnityEngine;

/// <summary>
/// 前方一块区域。矩形用长度和宽度，扇形用半径和张角，张角满 360 度是圆。
/// 存活期间按 tickInterval 结算。起点跟着持有者。持续喷射发射器还活着时，区域跟着它转。
/// </summary>
public class ZoneBullet : BulletBase
{
    private readonly List<Entity> _candidates = new List<Entity>();

    protected override float RepeatHitInterval
    {
        get
        {
            if (Config == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, Config.TickInterval);
        }
    }

    protected override void OnBirth()
    {
        CaptureFollowOffset();
    }

    protected override void OnTickBirth(float dt)
    {
        FollowAttachedOrigin();
        if (IsExpired())
        {
            ChangeToState(BulletStateCode.Death);
            return;
        }

        if (GatherZone() > 0)
        {
            ResolveHits();
        }
    }

    protected override bool ShouldDespawnAfterHit()
    {
        return false;
    }

    private int GatherZone()
    {
        ClearHits();
        if (Battle == null || Config == null || Params == null)
        {
            return 0;
        }

        Battle.CollectHittable(_candidates);
        int count = 0;
        for (int i = 0; i < _candidates.Count; i++)
        {
            Entity candidate = _candidates[i];
            if (!Contains(candidate))
            {
                continue;
            }

            if (AddHit(candidate))
            {
                count++;
            }
        }

        return count;
    }

    private bool Contains(Entity candidate)
    {
        float range = Params.MaxRange >= 0f ? Params.MaxRange : 1000f;
        Vector3 forward = CombatGeometry.FlattenXY(Direction);
        if (Config.ZoneShape == ZoneShape.Sector)
        {
            return CombatGeometry.IntersectsSector(Position, forward, candidate, range, Config.ConeAngle);
        }

        return CombatGeometry.IntersectsRect(Position, forward, candidate, range, Params.HitRadius);
    }
}
