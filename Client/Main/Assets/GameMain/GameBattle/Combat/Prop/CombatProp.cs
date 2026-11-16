using System.Collections.Generic;
using cfg.combat.dragon;
using Cysharp.Threading.Tasks;
using Dragon.Config;
using Dragon.Resource;
using UnityEngine;

/// <summary>
/// 战斗中生成的物件。伤害区域、分裂器和进出区域都是它的不同配置。
/// 周期伤害走自己持有的发射器和子弹；站在区域内才生效的 Buff 由物件自己挂上和卸掉。
/// </summary>
public class CombatProp : Entity
{
    private readonly List<Entity> _scan = new List<Entity>();
    private readonly List<Entity> _inside = new List<Entity>();
    private readonly List<Entity> _auraTargets = new List<Entity>();
    private readonly HashSet<int> _insideIds = new HashSet<int>();

    private int _faction = CombatFaction.Neutral;
    private float _activeTime;
    private bool _watchedEmitters;

    public combatProp Config { get; private set; }
    public Entity Source { get; private set; }
    public Vector3 AimDirection { get; private set; }

    public static CombatProp Create(CombatPropSpawnArgs args)
    {
        if (args == null)
        {
            Debug.LogError("CombatProp.Create fail, args is null.");
            return null;
        }

        combatProp config = ConfigManager.Instance.Tables?.TbcombatProp.GetOrDefault(args.ConfigId);
        if (config == null)
        {
            Debug.LogError($"CombatProp.Create fail, missing prop config id={args.ConfigId}");
            return null;
        }

        CombatProp prop = new CombatProp();
        prop.Setup(args, config);
        return prop;
    }

    protected override EntityType GetEntityType()
    {
        return EntityType.CombatProp;
    }

    protected override bool OnFSMSetup()
    {
        FSM.Register(new CombatPropBirthState());
        FSM.Register(new CombatPropIdleState());
        FSM.Register(new CombatPropDeathState());
        return true;
    }

    protected override int GetDefaultFactionId()
    {
        return _faction;
    }

    protected override void CollectInitialEmitterConfigIds(List<int> result)
    {
        if (Config == null || Config.EmitterIds == null)
        {
            return;
        }

        for (int i = 0; i < Config.EmitterIds.Length; i++)
        {
            if (Config.EmitterIds[i] > 0)
            {
                result.Add(Config.EmitterIds[i]);
            }
        }
    }

    protected override void OnUpdate(float dt)
    {
        if (IsDespawnPending)
        {
            return;
        }

        UpdateBuffs(dt);
        if (!IsCurrentState(EntityStateCode.Idle))
        {
            return;
        }

        UpdateEmitters(dt);
        TickActive(dt);
    }

    protected override bool OnUnInitBefore()
    {
        ClearAura();
        return base.OnUnInitBefore();
    }

    public void EnterDeath()
    {
        if (IsDespawnPending)
        {
            return;
        }

        ClearAura();
        IsDead = true;
        MarkDespawn();
    }

    private void Setup(CombatPropSpawnArgs args, combatProp config)
    {
        Config = config;
        Source = args.Source;
        AimDirection = CombatGeometry.FlattenXY(args.Direction);
        _faction = args.FactionId;
        _watchedEmitters = config.EmitterIds != null && config.EmitterIds.Length > 0;

        Data = new EntityData();
        Data.Position.SetLogicPosition(args.Position);
        BindBattleCombat();
        ChangeToState(EntityStateCode.Birth);

        if (!string.IsNullOrEmpty(config.PrefabName))
        {
            LoadSkin(config.PrefabName).Forget();
        }
    }

    private void TickActive(float dt)
    {
        _activeTime += dt;
        TickAura();

        if (Config.Duration >= 0f && _activeTime >= Config.Duration)
        {
            ChangeToState(EntityStateCode.Death);
            return;
        }

        if (_watchedEmitters && Config.DestroyWhenEmittersDone && Emitters.Count == 0)
        {
            ChangeToState(EntityStateCode.Death);
        }
    }

    private void TickAura()
    {
        if (Config.EnterBuffIds == null || Config.EnterBuffIds.Length == 0 || Config.Radius <= 0f || Battle == null)
        {
            ClearAura();
            return;
        }

        _inside.Clear();
        _insideIds.Clear();
        Battle.CollectHittable(_scan);
        Vector3 origin = Data.Position.LogicPosition;
        for (int i = 0; i < _scan.Count; i++)
        {
            Entity candidate = _scan[i];
            if (!CombatTargetUtility.PassFilter(this, candidate, Config.TargetRelation))
            {
                continue;
            }

            if (!CombatGeometry.InRange(origin, candidate, Config.Radius))
            {
                continue;
            }

            _inside.Add(candidate);
            _insideIds.Add(candidate.InstanceId);
        }

        if (Config.RemoveBuffOnExit)
        {
            for (int i = 0; i < _auraTargets.Count; i++)
            {
                Entity target = _auraTargets[i];
                if (target == null || _insideIds.Contains(target.InstanceId))
                {
                    continue;
                }

                target.Buffs?.RemoveFromCaster(this, Config.EnterBuffIds);
            }
        }

        for (int i = 0; i < _inside.Count; i++)
        {
            EnsureAura(_inside[i]);
        }

        _auraTargets.Clear();
        for (int i = 0; i < _inside.Count; i++)
        {
            _auraTargets.Add(_inside[i]);
        }
    }

    private void EnsureAura(Entity target)
    {
        if (target == null || target.Buffs == null)
        {
            return;
        }

        for (int i = 0; i < Config.EnterBuffIds.Length; i++)
        {
            int buffId = Config.EnterBuffIds[i];
            if (buffId > 0)
            {
                target.Buffs.AddFromSource(buffId, this);
            }
        }
    }

    private void ClearAura()
    {
        if (Config != null && Config.RemoveBuffOnExit && Config.EnterBuffIds != null)
        {
            for (int i = 0; i < _auraTargets.Count; i++)
            {
                Entity target = _auraTargets[i];
                if (target == null)
                {
                    continue;
                }

                target.Buffs?.RemoveFromCaster(this, Config.EnterBuffIds);
            }
        }

        _auraTargets.Clear();
    }

    private async UniTaskVoid LoadSkin(string prefabName)
    {
        GameObject prefab = await ResourceManager.Instance.LoadAssetAsync<GameObject>(prefabName);
        if (prefab == null || IsDespawnPending)
        {
            return;
        }

        GameObject skin = Object.Instantiate(prefab);
        if (IsDespawnPending)
        {
            Object.Destroy(skin);
            return;
        }

        Skin = skin;
        if (Dragon.Logic.MapSceneManager.Instance != null)
        {
            Dragon.Logic.MapSceneManager.Instance.EntityManager?.BindSkin(this);
        }
    }
}

public sealed class CombatPropSpawnArgs
{
    public int ConfigId;
    public Vector3 Position;
    public Vector3 Direction;
    public int FactionId;
    public Entity Source;
}

public static class CombatPropSpawner
{
    public static void SpawnFromBullet(BulletBase bullet, List<Entity> targets)
    {
        if (bullet == null || bullet.Config == null || bullet.Config.SpawnPropIds == null || bullet.Config.SpawnPropIds.Length == 0)
        {
            return;
        }

        EntityManager manager = Dragon.Logic.MapSceneManager.Instance != null
            ? Dragon.Logic.MapSceneManager.Instance.EntityManager
            : null;
        if (manager == null)
        {
            Debug.LogError("CombatPropSpawner.SpawnFromBullet fail, EntityManager is null.");
            return;
        }

        Entity source = bullet.Snapshot != null && bullet.Snapshot.Attacker != null ? bullet.Snapshot.Attacker : bullet.Owner;
        if (source is CombatProp prop && prop.Source != null)
        {
            source = prop.Source;
        }

        if (bullet.Config.SpawnPropAnchor == CombatPropSpawnAnchor.Target)
        {
            if (targets == null)
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                Entity target = targets[i];
                if (target == null || target.Data == null || target.Data.Position == null)
                {
                    continue;
                }

                SpawnAll(manager, bullet.Config.SpawnPropIds, target.Data.Position.LogicPosition, bullet.Direction, bullet.FactionId, source);
            }

            return;
        }

        SpawnAll(manager, bullet.Config.SpawnPropIds, bullet.Position, bullet.Direction, bullet.FactionId, source);
    }

    private static void SpawnAll(EntityManager manager, int[] propIds, Vector3 position, Vector3 direction, int factionId, Entity source)
    {
        for (int i = 0; i < propIds.Length; i++)
        {
            if (propIds[i] <= 0)
            {
                continue;
            }

            CombatProp prop = CombatProp.Create(new CombatPropSpawnArgs
            {
                ConfigId = propIds[i],
                Position = position,
                Direction = direction,
                FactionId = factionId,
                Source = source,
            });
            if (prop != null)
            {
                manager.AddEntity(prop);
            }
        }
    }
}
