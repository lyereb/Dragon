using cfg.combat.dragon;
using UnityEngine;

public static class CombatGeometry
{
    public static float DistanceXY(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dy = a.y - b.y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    public static float GetBodyRadius(Entity entity)
    {
        if (entity == null || entity.Data == null)
        {
            return 0f;
        }

        return Mathf.Max(entity.Data.WidthSize, entity.Data.DepthSize) * 0.5f;
    }

    public static bool InRange(Vector3 origin, Entity target, float radius)
    {
        if (target == null || target.Data == null || target.Data.Position == null)
        {
            return false;
        }

        float reach = Mathf.Max(0f, radius) + GetBodyRadius(target);
        return DistanceXY(origin, target.Data.Position.LogicPosition) <= reach;
    }

    public static float DistancePointToSegmentXY(Vector3 point, Vector3 start, Vector3 end)
    {
        float abx = end.x - start.x;
        float aby = end.y - start.y;
        float lengthSq = abx * abx + aby * aby;
        if (lengthSq <= 0.0001f)
        {
            return DistanceXY(point, start);
        }

        float tapx = point.x - start.x;
        float tapy = point.y - start.y;
        float t = (tapx * abx + tapy * aby) / lengthSq;
        if (t < 0f)
        {
            t = 0f;
        }
        else if (t > 1f)
        {
            t = 1f;
        }

        float closestX = start.x + abx * t;
        float closestY = start.y + aby * t;
        float dx = point.x - closestX;
        float dy = point.y - closestY;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    public static bool IntersectsSegment(Vector3 start, Vector3 end, Entity target, float width)
    {
        if (target == null || target.Data == null || target.Data.Position == null)
        {
            return false;
        }

        float reach = Mathf.Max(0f, width) + GetBodyRadius(target);
        return DistancePointToSegmentXY(target.Data.Position.LogicPosition, start, end) <= reach;
    }

    public static Vector3 RotateXY(Vector3 direction, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector3(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos,
            0f);
    }

    public static Vector3 FlattenXY(Vector3 direction)
    {
        direction.z = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return Vector3.right;
        }

        return direction.normalized;
    }

    public static float SignedAngleXY(Vector3 from, Vector3 to)
    {
        Vector3 a = FlattenXY(from);
        Vector3 b = FlattenXY(to);
        float dot = a.x * b.x + a.y * b.y;
        float cross = a.x * b.y - a.y * b.x;
        return Mathf.Atan2(cross, dot) * Mathf.Rad2Deg;
    }

    public static bool IntersectsSector(Vector3 origin, Vector3 forward, Entity target, float range, float coneAngle)
    {
        if (target == null || target.Data == null || target.Data.Position == null)
        {
            return false;
        }

        Vector3 point = target.Data.Position.LogicPosition;
        float dx = point.x - origin.x;
        float dy = point.y - origin.y;
        float distance = Mathf.Sqrt(dx * dx + dy * dy);
        float body = GetBodyRadius(target);
        if (distance > Mathf.Max(0f, range) + body)
        {
            return false;
        }

        if (coneAngle >= 359.9f || distance <= 0.0001f)
        {
            return true;
        }

        Vector3 dir = FlattenXY(forward);
        float dot = dir.x * dx + dir.y * dy;
        float cross = dir.x * dy - dir.y * dx;
        float degrees = Mathf.Atan2(Mathf.Abs(cross), dot) * Mathf.Rad2Deg;
        float slack = body > 0f ? Mathf.Atan2(body, Mathf.Max(distance, 0.0001f)) * Mathf.Rad2Deg : 0f;
        return degrees <= Mathf.Max(0f, coneAngle) * 0.5f + slack;
    }

    public static bool IntersectsRect(Vector3 origin, Vector3 forward, Entity target, float length, float halfWidth)
    {
        if (target == null || target.Data == null || target.Data.Position == null)
        {
            return false;
        }

        Vector3 dir = FlattenXY(forward);
        Vector3 point = target.Data.Position.LogicPosition;
        float dx = point.x - origin.x;
        float dy = point.y - origin.y;
        float along = dx * dir.x + dy * dir.y;
        float side = dir.x * dy - dir.y * dx;
        float len = Mathf.Max(0f, length);
        float half = Mathf.Max(0f, halfWidth);
        float clampedAlong = Mathf.Clamp(along, 0f, len);
        float clampedSide = Mathf.Clamp(side, -half, half);
        float ex = along - clampedAlong;
        float ey = side - clampedSide;
        float body = GetBodyRadius(target);
        return ex * ex + ey * ey <= body * body;
    }
}

public static class CombatFactionRule
{
    public static bool Match(int selfFaction, int otherFaction, TargetRelation relation)
    {
        switch (relation)
        {
            case TargetRelation.Ally:
                return selfFaction == otherFaction;
            case TargetRelation.Hostile:
                return otherFaction != CombatFaction.Neutral && selfFaction != otherFaction;
            case TargetRelation.Any:
                return true;
            default:
                return false;
        }
    }
}

public static class CombatTargetUtility
{
    public static bool IsHittable(Entity entity)
    {
        return entity != null
            && !entity.IsDead
            && entity is IAttackable
            && entity.Data != null
            && entity.Data.Position != null;
    }

    public static bool PassFilter(Entity self, Entity other, TargetRelation relation)
    {
        if (!IsHittable(other) || other == self)
        {
            return false;
        }

        int selfFaction = self != null ? self.FactionId : CombatFaction.Neutral;
        return CombatFactionRule.Match(selfFaction, other.FactionId, relation);
    }
}
