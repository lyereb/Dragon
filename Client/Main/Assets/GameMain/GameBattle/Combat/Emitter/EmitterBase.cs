using cfg.combat.dragon;
using Dragon.Config;
using Dragon.Library;
using UnityEngine;

/// <summary>
/// 发射器基类。由 Entity 创建并在 Entity.Update 里驱动。
/// 负责冷却、连发、弹药、生成点，以及把子弹交给 BulletManager。
/// </summary>
public abstract class EmitterBase : FSMHostBase<EmitterStateCode>
{
    private static int _nextInstanceId = 1;

    private int _ammo;
    private float _cooldown;
    private int _burstRemaining;
    private float _burstTimer;
    private bool _manualRequested;
    private bool _holdingShot;
    private bool _depleted;
    private GameObject _view;
    private bool _viewClosed;

    public int InstanceId { get; }
    public Entity Owner { get; private set; }
    public emitter Config { get; private set; }
    public BattleCombat Battle { get; private set; }
    public TargetQueryBase TargetQuery { get; private set; }
    public CombatModifierBoard CombatModifiers { get; }
    public bool IsShutdown { get; private set; }

    protected EmitterBase() : base("EmitterFSM")
    {
        InstanceId = _nextInstanceId++;
        CombatModifiers = new CombatModifierBoard();
    }

    public void Initialize(Entity owner, emitter config, BattleCombat battle)
    {
        Owner = owner;
        Config = config;
        Battle = battle;
        _ammo = config.Ammo;
        _cooldown = Mathf.Max(0f, config.StartCooldown);
        TargetQuery = TargetQueryFactory.Create(config.TargetQueryType);
        TargetQuery.Bind(this);

        if (battle != null && owner != null)
        {
            battle.GetFaction(owner.FactionId).AttachEmitter(this);
        }

        owner?.Buffs?.CatchUpEmitter(this);
        ChangeToState(EmitterStateCode.Birth);
        CombatView.Load(config.PrefabName, view =>
        {
            _view = view;
            SyncView();
        }, () => _viewClosed || IsShutdown);
    }

    public void Update(float dt)
    {
        if (IsShutdown || dt <= 0f)
        {
            return;
        }

        TargetQuery?.Update(dt);
        OnFSMUpdate(dt);
        SyncView();
    }

    public void NotifyAttackInput()
    {
        if (Config != null && Config.FireMode == EmitterFireMode.Manual)
        {
            _manualRequested = true;
        }
    }

    public void Shutdown()
    {
        if (IsShutdown)
        {
            return;
        }

        if (FSM != null && !IsCurrentState(EmitterStateCode.Death))
        {
            ChangeToState(EmitterStateCode.Death);
        }

        if (!IsShutdown)
        {
            EnterDeath();
        }
    }

    public void DisposeFSM()
    {
        DestroyFSM();
    }

    public float Resolve(CombatParam param, float baseValue)
    {
        if (CombatModifiers == null)
        {
            return baseValue;
        }

        return CombatModifiers.Resolve(param, baseValue);
    }

    public void TickReady(float dt)
    {
        if (Owner != null && Owner.IsDead)
        {
            ChangeToState(EmitterStateCode.Death);
            return;
        }

        if (_cooldown > 0f)
        {
            _cooldown -= dt;
        }

        if (_cooldown > 0f)
        {
            return;
        }

        if (Owner != null && Owner.BlocksAttack())
        {
            return;
        }

        if (Config.FireMode == EmitterFireMode.Manual && !_manualRequested)
        {
            return;
        }

        if (!HasAmmo())
        {
            if (Config.DestroyWhenAmmoEmpty)
            {
                ChangeToState(EmitterStateCode.Death);
            }
            return;
        }

        if (!CanStartFire())
        {
            return;
        }

        _manualRequested = false;
        ChangeToState(EmitterStateCode.Fire);
    }

    public void EnterFire()
    {
        _burstRemaining = Mathf.Max(1, Mathf.RoundToInt(Resolve(CombatParam.BurstCount, Config.BurstCount)));
        _burstTimer = 0f;
        _holdingShot = false;
    }

    public void TickFire(float dt)
    {
        if (Owner != null && Owner.IsDead)
        {
            ChangeToState(EmitterStateCode.Death);
            return;
        }

        if (Owner != null && Owner.BlocksAttack())
        {
            return;
        }

        if (_holdingShot)
        {
            TickHeldShot(dt);
            return;
        }

        if (_burstRemaining <= 0)
        {
            FinishVolley();
            return;
        }

        if (_burstTimer > 0f)
        {
            _burstTimer -= dt;
            if (_burstTimer > 0f)
            {
                return;
            }
        }

        if (!HasAmmo() || !TryFireOneShot())
        {
            FinishVolley();
            return;
        }

        if (BeginHeldShot())
        {
            _holdingShot = true;
            return;
        }

        _burstRemaining--;
        if (_burstRemaining <= 0 || _depleted)
        {
            FinishVolley();
            return;
        }

        _burstTimer = Mathf.Max(0f, Resolve(CombatParam.BurstInterval, Config.BurstInterval));
    }

    /// <summary>
    /// 这一发生成之后继续留在开火状态。持续喷射用来跟着索敌转向。
    /// </summary>
    protected virtual bool BeginHeldShot()
    {
        return false;
    }

    protected virtual void TickHeldShot(float dt)
    {
    }

    protected void CompleteHeldShot()
    {
        _holdingShot = false;
        _burstRemaining--;
        if (_burstRemaining <= 0 || _depleted)
        {
            FinishVolley();
            return;
        }

        _burstTimer = Mathf.Max(0f, Resolve(CombatParam.BurstInterval, Config.BurstInterval));
    }

    public void EnterDeath()
    {
        CloseView();
        if (IsShutdown)
        {
            return;
        }

        IsShutdown = true;
        TargetQuery?.ClearLock();
        if (Battle != null && Owner != null)
        {
            Battle.GetFaction(Owner.FactionId).DetachEmitter(this);
        }
    }

    private void SyncView()
    {
        if (_view == null)
        {
            return;
        }

        if (Owner != null && Owner.Skin != null)
        {
            if (_view.transform.parent != Owner.Skin.transform)
            {
                _view.transform.SetParent(Owner.Skin.transform, false);
                _view.transform.localPosition = Vector3.zero;
                _view.transform.localRotation = Quaternion.identity;
            }

            return;
        }

        if (Owner != null && Owner.Data != null && Owner.Data.Position != null)
        {
            CombatView.Sync(_view, Owner.Data.Position.LogicPosition);
        }
    }

    private void CloseView()
    {
        _viewClosed = true;
        CombatView.Destroy(ref _view);
    }

    protected virtual bool CanStartFire()
    {
        return TryPrepareShot(out _, out _);
    }

    protected abstract bool TryPrepareShot(out Vector3 direction, out Entity target);

    protected bool TryFireOneShot()
    {
        if (!HasAmmo())
        {
            _depleted = Config.DestroyWhenAmmoEmpty;
            return false;
        }

        if (!TryPrepareShot(out Vector3 direction, out Entity target))
        {
            return false;
        }

        direction = CombatGeometry.FlattenXY(direction);
        int projectileCount = Mathf.Max(1, Mathf.RoundToInt(Resolve(CombatParam.ProjectileCount, Config.ProjectileCount)));
        Vector3 origin = ComputeSpawnPosition(direction);
        float spread = Config.SpreadAngle;
        bool fullCircle = spread >= 359.9f && projectileCount > 1;
        for (int i = 0; i < projectileCount; i++)
        {
            float angle = projectileCount == 1
                ? 0f
                : fullCircle
                    ? spread * i / projectileCount
                    : -spread * 0.5f + spread * i / (projectileCount - 1);
            Vector3 shotDirection = CombatGeometry.RotateXY(direction, angle);
            SpawnBullet(origin, shotDirection, target);
        }

        ConsumeAmmo();
        return true;
    }

    protected Vector3 ComputeSpawnPosition(Vector3 aimDirection)
    {
        Vector3 origin = Owner != null && Owner.Data != null && Owner.Data.Position != null
            ? Owner.Data.Position.LogicPosition
            : Vector3.zero;
        Vector3 forward = CombatGeometry.FlattenXY(aimDirection);
        Vector3 side = new Vector3(-forward.y, forward.x, 0f);
        return origin
            + forward * Config.SpawnOffsetX
            + side * Config.SpawnOffsetY
            + new Vector3(0f, 0f, Config.SpawnOffsetZ);
    }

    protected override bool OnFSMSetup()
    {
        FSM.Register(new EmitterBirthState());
        FSM.Register(new EmitterReadyState());
        FSM.Register(new EmitterFireState());
        FSM.Register(new EmitterDeathState());
        return true;
    }

    private void SpawnBullet(Vector3 position, Vector3 direction, Entity target)
    {
        if (Battle == null || Battle.Bullets == null)
        {
            Debug.LogError($"Emitter {Config.Id} spawn bullet fail, BattleCombat is null.");
            return;
        }

        bullet bulletConfig = ConfigManager.Instance.Tables?.Tbbullet.GetOrDefault(Config.BulletId);
        if (bulletConfig == null)
        {
            Debug.LogError($"Emitter {Config.Id} spawn bullet fail, bullet config {Config.BulletId} is missing.");
            return;
        }

        BulletSpawnArgs args = new BulletSpawnArgs
        {
            Owner = Owner,
            Emitter = this,
            Config = bulletConfig,
            Params = BakeBulletParams(bulletConfig),
            Snapshot = AttackerSnapshot.Capture(Owner, this),
            Position = position,
            Direction = CombatGeometry.FlattenXY(direction),
            PreferredTarget = target,
            FactionId = Owner != null ? Owner.FactionId : CombatFaction.Neutral,
            Relation = Config.TargetRelation,
        };
        Battle.Bullets.Spawn(args);
    }

    private BulletRuntimeParams BakeBulletParams(bullet bulletConfig)
    {
        FactionCombatContext faction = Battle.GetFaction(Owner.FactionId);
        return new BulletRuntimeParams
        {
            Speed = Mathf.Max(0f, BakeFloat(CombatParam.BulletSpeed, bulletConfig.Speed, faction, false)),
            LifeTime = BakeFloat(CombatParam.BulletLifeTime, bulletConfig.LifeTime, faction, true),
            MaxRange = BakeFloat(CombatParam.BulletMaxRange, bulletConfig.MaxRange, faction, true),
            HitRadius = Mathf.Max(0f, BakeFloat(CombatParam.BulletHitRadius, bulletConfig.HitRadius, faction, false)),
            Pierce = BakePierce(bulletConfig.Pierce, faction),
            DamageRatio = Mathf.Max(0f, BakeFloat(CombatParam.BulletDamageRatio, bulletConfig.DamageRatio, faction, false)),
            FlatDamage = BakeFloat(CombatParam.BulletFlatDamage, bulletConfig.FlatDamage, faction, false),
            FixedDamage = Mathf.Max(0f, BakeFloat(CombatParam.BulletFixedDamage, bulletConfig.FixedDamage, faction, false)),
            ExtraCritRate = bulletConfig.ExtraCritRate,
            CritMultiplier = bulletConfig.CritMultiplier,
        };
    }

    private float BakeFloat(CombatParam param, float baseValue, FactionCombatContext faction, bool keepNegativeInfinite)
    {
        bool modified = CombatModifiers.Has(param) || (faction != null && faction.HasBulletParam(param));
        if (keepNegativeInfinite && baseValue < 0f && !modified)
        {
            return -1f;
        }

        float flat = 0f;
        float percent = 0f;
        CombatModifiers.Accumulate(param, ref flat, ref percent);
        faction?.AccumulateBullet(param, ref flat, ref percent);
        return CombatModifierMath.Apply(baseValue, flat, percent);
    }

    private int BakePierce(int baseValue, FactionCombatContext faction)
    {
        bool modified = CombatModifiers.Has(CombatParam.BulletPierce) || (faction != null && faction.HasBulletParam(CombatParam.BulletPierce));
        if (baseValue < 0 && !modified)
        {
            return -1;
        }

        float resolved = BakeFloat(CombatParam.BulletPierce, baseValue, faction, false);
        return Mathf.RoundToInt(resolved);
    }

    private bool HasAmmo()
    {
        return Config.Ammo < 0 || _ammo > 0;
    }

    private void ConsumeAmmo()
    {
        if (Config.Ammo < 0)
        {
            return;
        }

        _ammo--;
        if (_ammo <= 0 && Config.DestroyWhenAmmoEmpty)
        {
            _depleted = true;
        }
    }

    private void FinishVolley()
    {
        _cooldown = Mathf.Max(0f, Resolve(CombatParam.FireInterval, Config.FireInterval));
        if (_depleted || (!HasAmmo() && Config.DestroyWhenAmmoEmpty))
        {
            ChangeToState(EmitterStateCode.Death);
            return;
        }

        ChangeToState(EmitterStateCode.Ready);
    }
}

/// <summary>
/// 朝索敌主目标的方向发射。没有目标时停留在准备，不消耗冷却。
/// </summary>
public class DirectionalShotEmitter : EmitterBase
{
    protected override bool TryPrepareShot(out Vector3 direction, out Entity target)
    {
        direction = Vector3.right;
        target = TargetQuery != null ? TargetQuery.GetPrimaryTarget() : null;
        if (target == null || target.Data == null || target.Data.Position == null)
        {
            return false;
        }

        if (Owner == null || Owner.Data == null || Owner.Data.Position == null)
        {
            return false;
        }

        Vector3 delta = target.Data.Position.LogicPosition - Owner.Data.Position.LogicPosition;
        direction = CombatGeometry.FlattenXY(delta);
        return true;
    }
}

/// <summary>
/// 持续喷射。开火后停在这一发里，按索敌更新朝向，直到子弹配置的存活时间结束。
/// 区域子弹读 AimDirection 跟着转。没有目标时，战斗物件改用自己的瞄准方向。
/// </summary>
public class SustainedSprayEmitter : EmitterBase
{
    private float _sprayRemain;
    private bool _sprayEndless;

    public Vector3 AimDirection { get; private set; } = Vector3.right;

    protected override bool TryPrepareShot(out Vector3 direction, out Entity target)
    {
        direction = Vector3.right;
        if (!RefreshAim(true, out target))
        {
            return false;
        }

        direction = AimDirection;
        if (target != null)
        {
            return target.Data != null && target.Data.Position != null;
        }

        return Owner is CombatProp;
    }

    protected override bool BeginHeldShot()
    {
        bullet bulletConfig = ConfigManager.Instance.Tables?.Tbbullet.GetOrDefault(Config.BulletId);
        float life = bulletConfig != null ? bulletConfig.LifeTime : 0f;
        _sprayEndless = life < 0f;
        _sprayRemain = _sprayEndless ? 0f : life;
        return true;
    }

    protected override void TickHeldShot(float dt)
    {
        RefreshAim(false, out _);
        if (_sprayEndless)
        {
            return;
        }

        _sprayRemain -= dt;
        if (_sprayRemain > 0f)
        {
            return;
        }

        CompleteHeldShot();
    }

    private bool RefreshAim(bool requireTarget, out Entity target)
    {
        target = TargetQuery != null ? TargetQuery.GetPrimaryTarget() : null;
        if (target != null && target.Data != null && target.Data.Position != null
            && Owner != null && Owner.Data != null && Owner.Data.Position != null)
        {
            Vector3 delta = target.Data.Position.LogicPosition - Owner.Data.Position.LogicPosition;
            AimDirection = CombatGeometry.FlattenXY(delta);
            return true;
        }

        target = null;
        CombatProp prop = Owner as CombatProp;
        if (prop != null)
        {
            AimDirection = CombatGeometry.FlattenXY(prop.AimDirection);
            return true;
        }

        return !requireTarget;
    }
}

/// <summary>
/// 沿持有者瞄准方向散射。没有目标也开火，命中交给子弹自己判断。
/// </summary>
public class SpreadBurstEmitter : EmitterBase
{
    protected override bool TryPrepareShot(out Vector3 direction, out Entity target)
    {
        target = null;
        CombatProp prop = Owner as CombatProp;
        direction = prop != null ? CombatGeometry.FlattenXY(prop.AimDirection) : Vector3.right;
        return Owner != null;
    }
}

public static class EmitterFactory
{
    public static EmitterBase Create(Entity owner, int emitterConfigId, BattleCombat battle)
    {
        emitter config = ConfigManager.Instance.Tables?.Tbemitter.GetOrDefault(emitterConfigId);
        if (config == null)
        {
            Debug.LogError($"EmitterFactory.Create fail, missing emitter config id={emitterConfigId}");
            return null;
        }

        EmitterBase emitter;
        switch (config.EmitterType)
        {
            case EmitterType.DirectionalShot:
                emitter = new DirectionalShotEmitter();
                break;
            case EmitterType.SpreadBurst:
                emitter = new SpreadBurstEmitter();
                break;
            case EmitterType.SustainedSpray:
                emitter = new SustainedSprayEmitter();
                break;
            default:
                Debug.LogError($"EmitterFactory.Create fail, unknown emitter type={config.EmitterType}, id={config.Id}");
                return null;
        }

        emitter.Initialize(owner, config, battle);
        return emitter;
    }
}

public sealed class BulletRuntimeParams
{
    public float Speed;
    public float LifeTime;
    public float MaxRange;
    public float HitRadius;
    public int Pierce;
    public float DamageRatio;
    public float FlatDamage;
    public float FixedDamage;
    public float ExtraCritRate;
    public float CritMultiplier;
}

public sealed class AttackerSnapshot
{
    public Entity Attacker;
    public int FactionId;
    public int EmitterConfigId;
    public float Attack;
    public float DamageDealtRatio = 1f;
    public float HealDealtRatio = 1f;
    public float CritRate;
    public float CritMultiplier = 1.5f;

    public static AttackerSnapshot Capture(Entity attacker, EmitterBase emitter)
    {
        Entity statSource = attacker;
        if (attacker is CombatProp prop && prop.Source != null)
        {
            statSource = prop.Source;
        }

        float attack = 0f;
        CharacterData data = statSource != null ? statSource.GetData<CharacterData>() : null;
        if (data != null)
        {
            attack = data.GetCurrentAttribute(cfg.entity.dragon.CharacterAttr.ATTACK);
        }

        float dealt = 1f;
        float healDealt = 1f;
        float critRate = 0f;
        float critMultiplier = 1.5f;
        if (statSource != null && statSource.CombatModifiers != null)
        {
            dealt = statSource.CombatModifiers.Resolve(CombatParam.DamageDealtRatio, 1f);
            healDealt = statSource.CombatModifiers.Resolve(CombatParam.HealDealtRatio, 1f);
            critRate = statSource.CombatModifiers.Resolve(CombatParam.CritRate, 0f);
            critMultiplier = statSource.CombatModifiers.Resolve(CombatParam.CritMultiplier, 1.5f);
        }

        return new AttackerSnapshot
        {
            Attacker = statSource != null ? statSource : attacker,
            FactionId = attacker != null ? attacker.FactionId : CombatFaction.Neutral,
            EmitterConfigId = emitter != null && emitter.Config != null ? emitter.Config.Id : 0,
            Attack = attack,
            DamageDealtRatio = dealt,
            HealDealtRatio = healDealt,
            CritRate = critRate,
            CritMultiplier = critMultiplier,
        };
    }
}

public sealed class BulletSpawnArgs
{
    public Entity Owner;
    public EmitterBase Emitter;
    public bullet Config;
    public BulletRuntimeParams Params;
    public AttackerSnapshot Snapshot;
    public Vector3 Position;
    public Vector3 Direction;
    public Entity PreferredTarget;
    public int FactionId;
    public TargetRelation Relation = TargetRelation.Hostile;
}
