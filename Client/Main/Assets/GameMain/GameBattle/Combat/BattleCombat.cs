using System.Collections.Generic;
using cfg.combat.dragon;
using Dragon.Config;

/// <summary>
/// 一场战斗的公共入口：子弹管理器，以及每个阵营的局内修改器。
/// 由 MapSceneManager 创建，Entity 和发射器都从这里取。
/// </summary>
public sealed class BattleCombat
{
    private readonly Dictionary<int, FactionCombatContext> _factions = new Dictionary<int, FactionCombatContext>();
    private EntityManager _entityManager;

    public BulletManager Bullets { get; private set; }

    public void Initialize(EntityManager entityManager)
    {
        _entityManager = entityManager;
        Bullets = new BulletManager();
        _factions.Clear();
    }

    public void Update(float dt)
    {
        Bullets?.Update(dt);
    }

    public void Clear()
    {
        Bullets?.Clear();
        foreach (KeyValuePair<int, FactionCombatContext> pair in _factions)
        {
            pair.Value.Clear();
        }

        _factions.Clear();
        _entityManager = null;
        Bullets = null;
    }

    public FactionCombatContext GetFaction(int factionId)
    {
        if (!_factions.TryGetValue(factionId, out FactionCombatContext context))
        {
            context = new FactionCombatContext(factionId);
            _factions.Add(factionId, context);
        }

        return context;
    }

    public void AttachEntity(Entity entity)
    {
        if (entity == null)
        {
            return;
        }

        GetFaction(entity.FactionId).AttachEntity(entity);
    }

    public void DetachEntity(Entity entity)
    {
        if (entity == null)
        {
            return;
        }

        if (_factions.TryGetValue(entity.FactionId, out FactionCombatContext context))
        {
            context.DetachEntity(entity);
        }
    }

    public int AddRunModifier(int factionId, int modifierConfigId)
    {
        modifier config = ConfigManager.Instance.Tables?.Tbmodifier.GetOrDefault(modifierConfigId);
        CombatModifierSpec spec = CombatModifierSpec.FromConfig(config, 0, 1f);
        if (spec == null)
        {
            return 0;
        }

        return GetFaction(factionId).AddGrant(spec);
    }

    public bool RemoveRunModifier(int factionId, int grantId)
    {
        if (!_factions.TryGetValue(factionId, out FactionCombatContext context))
        {
            return false;
        }

        return context.RemoveGrant(grantId);
    }

    public void CollectHittable(List<Entity> results)
    {
        results.Clear();
        if (_entityManager == null)
        {
            return;
        }

        IReadOnlyList<Entity> entities = _entityManager.Entities;
        for (int i = 0; i < entities.Count; i++)
        {
            Entity entity = entities[i];
            if (CombatTargetUtility.IsHittable(entity))
            {
                results.Add(entity);
            }
        }
    }
}
