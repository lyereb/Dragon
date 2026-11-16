using System.Collections.Generic;
using cfg.combat.dragon;
using Dragon.Library;
using UnityEngine;

/// <summary>
/// 子弹基类。只能由发射器经 BulletManager 生成。
/// 生成时烤好攻击者快照和子弹参数，之后只由 BulletManager 更新。
/// </summary>
public abstract class BulletBase : FSMHostBase<BulletStateCode>
{
    public const int MaxHitsPerSweep = 32;

    private readonly List<Entity> _scan = new List<Entity>();
    private readonly List<Entity> _hits = new List<Entity>();
    private readonly List<Entity> _spawnTargets = new List<Entity>();
    private readonly Dictionary<int, float> _nextHitTime = new Dictionary<int, float>();
    private bool _pendingLaunch;
    private bool _born;
    private float _age;
    private float _traveled;
    private GameObject _view;
    private bool _viewClosed;
    private bool _followAttached;
    private float _followForward;
    private float _followSide;
    private float _followHeight;
    private float _channelYaw;

    public int InstanceId { get; private set; }
    public Entity Owner { get; private set; }
    public int FactionId { get; private set; }
    public bullet Config { get; private set; }
    public BulletRuntimeParams Params { get; private set; }
    public AttackerSnapshot Snapshot { get; private set; }
    public CombatModifierBoard CombatModifiers { get; }
    public Vector3 Position { get; protected set; }
    public Vector3 Direction { get; protected set; }
    public Entity PreferredTarget { get; protected set; }
    public TargetRelation Relation { get; private set; }
    public BattleCombat Battle { get; private set; }
    protected EmitterBase Emitter { get; private set; }
    public bool IsFinished { get; private set; }
    public int PierceRemaining { get; private set; }

    protected BulletBase() : base("BulletFSM")
    {
        CombatModifiers = new CombatModifierBoard();
    }

    public void AssignInstanceId(int instanceId)
    {
        InstanceId = instanceId;
    }

    public void Initialize(BulletSpawnArgs args)
    {
        Owner = args.Owner;
        FactionId = args.FactionId;
        Config = args.Config;
        Params = args.Params ?? new BulletRuntimeParams();
        Snapshot = args.Snapshot;
        Position = args.Position;
        Direction = CombatGeometry.FlattenXY(args.Direction);
        PreferredTarget = args.PreferredTarget;
        Relation = args.Relation;
        Emitter = args.Emitter;
        _channelYaw = 0f;
        if (args.Emitter is SustainedSprayEmitter spray)
        {
            _channelYaw = CombatGeometry.SignedAngleXY(spray.AimDirection, Direction);
        }

        Battle = args.Owner != null ? args.Owner.Battle : null;
        if (Battle == null && args.Emitter != null)
        {
            Battle = args.Emitter.Battle;
        }

        PierceRemaining = Params.Pierce;
        CombatView.Load(Config.PrefabName, view =>
        {
            _view = view;
            SyncView();
        }, () => _viewClosed || IsFinished);
    }

    public void Arm()
    {
        _pendingLaunch = true;
    }

    public void Update(float dt)
    {
        if (IsFinished)
        {
            return;
        }

        if (_pendingLaunch)
        {
            _pendingLaunch = false;
            ChangeToState(BulletStateCode.Birth);
        }

        if (!IsFinished)
        {
            OnFSMUpdate(dt);
        }

        SyncView();
    }

    public void Shutdown()
    {
        if (!IsFinished)
        {
            if (FSM != null && !IsCurrentState(BulletStateCode.Death))
            {
                ChangeToState(BulletStateCode.Death);
            }

            if (!IsFinished)
            {
                EnterDeath();
            }
        }

        DestroyFSM();
    }

    public void DisposeFSM()
    {
        DestroyFSM();
    }

    public void EnterBirth()
    {
        if (_born || IsFinished)
        {
            return;
        }

        _born = true;
        if (PierceRemaining == 0)
        {
            ChangeToState(BulletStateCode.Death);
            return;
        }

        OnBirth();
    }

    public void TickBirth(float dt)
    {
        if (IsFinished || !IsCurrentState(BulletStateCode.Birth))
        {
            return;
        }

        _age += dt;
        OnTickBirth(dt);
    }

    public void EnterHit()
    {
        int applied = ResolveHits();
        if (PierceRemaining > 0 && applied > 0)
        {
            PierceRemaining -= applied;
            if (PierceRemaining < 0)
            {
                PierceRemaining = 0;
            }
        }

        if (ShouldDespawnAfterHit())
        {
            ChangeToState(BulletStateCode.Death);
            return;
        }

        ChangeToState(BulletStateCode.Birth);
    }

    public void EnterDeath()
    {
        CloseView();
        IsFinished = true;
    }

    private void SyncView()
    {
        CombatView.Sync(_view, Position);
    }

    private void CloseView()
    {
        _viewClosed = true;
        CombatView.Destroy(ref _view);
    }

    protected abstract void OnBirth();

    protected virtual void OnTickBirth(float dt)
    {
    }

    /// <summary>
    /// 这一批目标已经结算完。连锁在这里选下一跳。列表在方法返回后会被清空。
    /// </summary>
    protected virtual void OnHitsApplied(List<Entity> targets)
    {
    }

    protected abstract bool ShouldDespawnAfterHit();

    /// <summary>
    /// 同一目标多久之后可以再结算。小于 0 表示这颗子弹永远不再打它。
    /// </summary>
    protected virtual float RepeatHitInterval => -1f;

    protected int GatherHits()
    {
        _hits.Clear();
        if (Battle == null || Config == null || Params == null)
        {
            return 0;
        }

        if (Config.HitMode == BulletHitMode.Single)
        {
            return GatherSingle();
        }

        return GatherArea();
    }

    protected bool IsExpired()
    {
        float lifeTime = Params != null ? Params.LifeTime : 0f;
        return lifeTime >= 0f && _age >= lifeTime;
    }

    protected void Move(float dt, out float moved)
    {
        float speed = CombatModifiers.Resolve(CombatParam.BulletSpeed, Params.Speed);
        if (speed < 0f)
        {
            speed = 0f;
        }

        Vector3 delta = Direction * speed * dt;
        delta.z = 0f;
        Position += delta;
        moved = delta.magnitude;
        _traveled += moved;
    }

    protected bool IsOutOfRange()
    {
        return Params.MaxRange >= 0f && _traveled >= Params.MaxRange;
    }

    protected override bool OnFSMSetup()
    {
        FSM.Register(new BulletBirthState());
        FSM.Register(new BulletHitState());
        FSM.Register(new BulletDeathState());
        return true;
    }

    private int GatherSingle()
    {
        if (PreferredTarget != null && CanHit(PreferredTarget) && CombatGeometry.InRange(Position, PreferredTarget, Params.HitRadius))
        {
            _hits.Add(PreferredTarget);
            return 1;
        }

        if (Config.BulletType == BulletType.Melee && PreferredTarget != null)
        {
            return 0;
        }

        Entity nearest = FindNearest();
        if (nearest == null)
        {
            return 0;
        }

        _hits.Add(nearest);
        return 1;
    }

    private int GatherArea()
    {
        FillInRadius(_scan);
        SortByDistance(_scan);
        int limit = PierceRemaining < 0 ? MaxHitsPerSweep : Mathf.Min(MaxHitsPerSweep, PierceRemaining);
        int count = Mathf.Min(limit, _scan.Count);
        for (int i = 0; i < count; i++)
        {
            _hits.Add(_scan[i]);
        }

        return _hits.Count;
    }

    private Entity FindNearest()
    {
        FillInRadius(_scan);
        Entity nearest = null;
        float best = float.MaxValue;
        for (int i = 0; i < _scan.Count; i++)
        {
            float distance = CombatGeometry.DistanceXY(Position, _scan[i].Data.Position.LogicPosition);
            if (distance < best)
            {
                best = distance;
                nearest = _scan[i];
            }
        }

        return nearest;
    }

    private readonly List<Entity> _radiusBuffer = new List<Entity>();

    private void FillInRadius(List<Entity> results)
    {
        results.Clear();
        Battle.CollectHittable(_radiusBuffer);
        for (int i = 0; i < _radiusBuffer.Count; i++)
        {
            Entity candidate = _radiusBuffer[i];
            if (!CanHit(candidate))
            {
                continue;
            }

            if (!CombatGeometry.InRange(Position, candidate, Params.HitRadius))
            {
                continue;
            }

            results.Add(candidate);
        }
    }

    protected bool CanHit(Entity target)
    {
        if (!CombatTargetUtility.PassFilter(Owner, target, Relation))
        {
            return false;
        }

        if (target != null && _nextHitTime.TryGetValue(target.InstanceId, out float allow) && _age <= allow)
        {
            return false;
        }

        return true;
    }

    protected void ClearHits()
    {
        _hits.Clear();
    }

    protected bool AddHit(Entity target)
    {
        if (target == null || !CanHit(target))
        {
            return false;
        }

        _hits.Add(target);
        return true;
    }

    protected int ResolveHits()
    {
        _spawnTargets.Clear();
        int applied = ApplyHits();
        if (applied > 0)
        {
            CombatPropSpawner.SpawnFromBullet(this, _spawnTargets);
        }

        OnHitsApplied(_spawnTargets);
        _hits.Clear();
        _spawnTargets.Clear();
        return applied;
    }

    private int ApplyHits()
    {
        int applied = 0;
        for (int i = 0; i < _hits.Count; i++)
        {
            Entity target = _hits[i];
            if (target == null || !CanHit(target))
            {
                continue;
            }

            RememberHit(target);
            if (CombatDamageSettlement.TryApplyBulletHit(this, target))
            {
                applied++;
                _spawnTargets.Add(target);
            }
        }

        return applied;
    }

    private void RememberHit(Entity target)
    {
        float interval = RepeatHitInterval;
        float allow = interval < 0f ? float.PositiveInfinity : _age + Mathf.Max(0f, interval);
        _nextHitTime[target.InstanceId] = allow;
    }

    private static void SortByDistance(List<Entity> entities, Vector3 origin)
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

    private void SortByDistance(List<Entity> entities)
    {
        SortByDistance(entities, Position);
    }

    protected void CaptureFollowOffset()
    {
        _followAttached = false;
        if (Owner == null || Owner.Data == null || Owner.Data.Position == null)
        {
            return;
        }

        Vector3 forward = CombatGeometry.FlattenXY(Direction);
        Vector3 side = new Vector3(-forward.y, forward.x, 0f);
        Vector3 origin = Owner.Data.Position.LogicPosition;
        Vector3 delta = Position - origin;
        _followForward = delta.x * forward.x + delta.y * forward.y;
        _followSide = delta.x * side.x + delta.y * side.y;
        _followHeight = Position.z - origin.z;
        _followAttached = true;
    }

    protected void FollowAttachedOrigin()
    {
        if (!_followAttached || Owner == null || Owner.IsDead || Owner.Data == null || Owner.Data.Position == null)
        {
            return;
        }

        Vector3 forward = Direction;
        if (Emitter is SustainedSprayEmitter spray && !spray.IsShutdown)
        {
            forward = CombatGeometry.RotateXY(spray.AimDirection, _channelYaw);
        }
        else
        {
            CombatProp prop = Owner as CombatProp;
            if (prop != null)
            {
                forward = prop.AimDirection;
            }
        }

        forward = CombatGeometry.FlattenXY(forward);
        Vector3 side = new Vector3(-forward.y, forward.x, 0f);
        Vector3 origin = Owner.Data.Position.LogicPosition;
        Vector3 position = origin + forward * _followForward + side * _followSide;
        Position = new Vector3(position.x, position.y, origin.z + _followHeight);
        Direction = forward;
    }
}
