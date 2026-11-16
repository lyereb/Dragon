using System.Collections.Generic;
using cfg.combat.dragon;
using Dragon.Config;
using UnityEngine;

/// <summary>
/// Buff 基类。管持续时间、同名覆盖、层数和周期回调。
/// 数值效果配修改器 id；Dot 一类的周期逻辑以后在子类 OnTick 里写。
/// </summary>
public abstract class BuffBase
{
    private static int _nextInstanceId = 1;

    private readonly ModifierLease _entityLease = new ModifierLease();
    private readonly Dictionary<int, ModifierLease> _emitterLeases = new Dictionary<int, ModifierLease>();
    private float _tickTimer;
    private int _tickIndex;

    public int InstanceId { get; private set; }
    public buff Config { get; private set; }
    public Entity Host { get; private set; }
    public Entity Caster { get; private set; }
    public string Name { get; private set; }
    public int Stack { get; private set; }
    public float RemainTime { get; private set; }
    public bool IsFinished { get; private set; }

    public void Setup(Entity host, Entity caster, buff config)
    {
        InstanceId = _nextInstanceId++;
        Host = host;
        Caster = caster;
        Config = config;
        Name = string.IsNullOrEmpty(config.Name) ? config.Id.ToString() : config.Name;
        Stack = 1;
        RemainTime = config.Duration;
        ApplyModifiers();
        ApplyRules();
        OnApply();
    }

    public void Update(float dt)
    {
        if (IsFinished || dt <= 0f)
        {
            return;
        }

        if (Config.TickInterval > 0f)
        {
            _tickTimer += dt;
            while (_tickTimer >= Config.TickInterval)
            {
                _tickTimer -= Config.TickInterval;
                _tickIndex++;
                CombatDamageSettlement.TryApplyBuffTick(this);
                OnTick(_tickIndex);
                if (IsFinished)
                {
                    return;
                }
            }
        }

        if (Config.Duration < 0f)
        {
            return;
        }

        RemainTime -= dt;
        if (RemainTime <= 0f)
        {
            Finish(BuffRemoveReason.Expired);
        }
    }

    public void ExtendDuration()
    {
        if (IsFinished)
        {
            return;
        }

        if (Config.Duration < 0f || RemainTime < 0f)
        {
            RemainTime = -1f;
        }
        else
        {
            RemainTime += Config.Duration;
        }

        OnRefresh();
    }

    public void AddStack()
    {
        if (IsFinished)
        {
            return;
        }

        int maxStack = Mathf.Max(1, Config.MaxStack);
        if (Stack < maxStack)
        {
            int previous = Stack;
            Stack++;
            ApplyModifiers();
            OnStackChanged(previous);
        }

        if (Config.Duration < 0f)
        {
            RemainTime = -1f;
        }
        else
        {
            RemainTime = Config.Duration;
        }

        OnRefresh();
    }

    public void Finish(BuffRemoveReason reason)
    {
        if (IsFinished)
        {
            return;
        }

        IsFinished = true;
        ReleaseModifiers();
        RemoveRules();
        OnRemove(reason);
    }

    public void CatchUpEmitter(EmitterBase emitter)
    {
        if (IsFinished || emitter == null || _emitterLeases.ContainsKey(emitter.InstanceId))
        {
            return;
        }

        ModifierLease lease = new ModifierLease();
        if (!ApplySpecsToEmitter(emitter, lease))
        {
            return;
        }

        _emitterLeases.Add(emitter.InstanceId, lease);
    }

    protected virtual void OnApply()
    {
    }

    protected virtual void OnRefresh()
    {
    }

    protected virtual void OnStackChanged(int previousStack)
    {
    }

    protected virtual void OnTick(int tickIndex)
    {
    }

    protected virtual void OnRemove(BuffRemoveReason reason)
    {
    }

    private void ApplyRules()
    {
        if (Host == null || Config == null || Config.Rules == null)
        {
            return;
        }

        for (int i = 0; i < Config.Rules.Length; i++)
        {
            Host.AddBuffRule(Config.Rules[i]);
        }
    }

    private void RemoveRules()
    {
        if (Host == null || Config == null || Config.Rules == null)
        {
            return;
        }

        for (int i = 0; i < Config.Rules.Length; i++)
        {
            Host.RemoveBuffRule(Config.Rules[i]);
        }
    }

    private void ApplyModifiers()
    {
        ReleaseModifiers();
        if (Config.ModifierIds == null || Host == null)
        {
            return;
        }

        for (int i = 0; i < Config.ModifierIds.Length; i++)
        {
            CombatModifierSpec spec = CreateSpec(Config.ModifierIds[i]);
            if (spec == null || spec.Scope != CombatModifierScope.Entity)
            {
                continue;
            }

            CombatModifierRouter.ApplyToEntity(Host, spec, _entityLease);
        }

        IReadOnlyList<EmitterBase> emitters = Host.Emitters;
        if (emitters == null)
        {
            return;
        }

        for (int i = 0; i < emitters.Count; i++)
        {
            CatchUpEmitter(emitters[i]);
        }
    }

    private bool ApplySpecsToEmitter(EmitterBase emitter, ModifierLease lease)
    {
        if (Config.ModifierIds == null)
        {
            return false;
        }

        bool applied = false;
        for (int i = 0; i < Config.ModifierIds.Length; i++)
        {
            CombatModifierSpec spec = CreateSpec(Config.ModifierIds[i]);
            if (spec == null || spec.Scope == CombatModifierScope.Entity)
            {
                continue;
            }

            CombatModifierRouter.ApplyToEmitter(emitter, spec, lease);
            applied = true;
        }

        return applied;
    }

    private CombatModifierSpec CreateSpec(int modifierId)
    {
        modifier config = ConfigManager.Instance.Tables?.Tbmodifier.GetOrDefault(modifierId);
        if (config == null)
        {
            Debug.LogError($"Buff {Config.Id} missing modifier config id={modifierId}");
            return null;
        }

        return CombatModifierSpec.FromConfig(config, InstanceId, Stack);
    }

    private void ReleaseModifiers()
    {
        _entityLease.Release();
        foreach (KeyValuePair<int, ModifierLease> pair in _emitterLeases)
        {
            pair.Value.Release();
        }

        _emitterLeases.Clear();
    }
}

/// <summary>
/// 表格类型 Generic 的空实现。只提供持续时间、覆盖和修改器。
/// </summary>
public class GenericBuff : BuffBase
{
}

public static class BuffFactory
{
    public static BuffBase Create(buff config)
    {
        if (config == null)
        {
            return null;
        }

        switch (config.BuffType)
        {
            case BuffType.Generic:
                return new GenericBuff();
            default:
                Debug.LogError($"BuffFactory.Create fail, unknown buff type={config.BuffType}, id={config.Id}");
                return null;
        }
    }
}

/// <summary>
/// 挂在 Entity 上的 Buff 容器。同名判定使用 Buff.Name。
/// </summary>
public sealed class BuffManager
{
    private readonly Entity _host;
    private readonly List<BuffBase> _buffs = new List<BuffBase>();

    public BuffManager(Entity host)
    {
        _host = host;
    }

    public IReadOnlyList<BuffBase> Buffs => _buffs;

    /// <summary>
    /// 区域一类的来源使用。同一个施加者和同一个配置只保留一份，不走同名覆盖。
    /// </summary>
    public BuffBase AddFromSource(int configId, Entity caster)
    {
        buff config = ConfigManager.Instance.Tables?.Tbbuff.GetOrDefault(configId);
        if (config == null)
        {
            Debug.LogError($"BuffManager.AddFromSource fail, missing buff config id={configId}");
            return null;
        }

        BuffBase existing = FindLiving(configId, caster);
        if (existing != null)
        {
            return existing;
        }

        return CreateAndAdd(config, caster);
    }

    public void RemoveFromCaster(Entity caster, int[] configIds)
    {
        if (caster == null || configIds == null)
        {
            return;
        }

        for (int i = _buffs.Count - 1; i >= 0; i--)
        {
            BuffBase buff = _buffs[i];
            if (buff.Caster != caster || buff.Config == null || !ContainsConfigId(configIds, buff.Config.Id))
            {
                continue;
            }

            buff.Finish(BuffRemoveReason.SourceEnded);
            _buffs.RemoveAt(i);
        }
    }

    public BuffBase Add(int configId, Entity caster)
    {
        buff config = ConfigManager.Instance.Tables?.Tbbuff.GetOrDefault(configId);
        if (config == null)
        {
            Debug.LogError($"BuffManager.Add fail, missing buff config id={configId}");
            return null;
        }

        string name = string.IsNullOrEmpty(config.Name) ? config.Id.ToString() : config.Name;
        BuffBase existing = FindByName(name);
        if (existing == null)
        {
            return CreateAndAdd(config, caster);
        }

        switch (config.OverlapMode)
        {
            case BuffOverlapMode.Replace:
                existing.Finish(BuffRemoveReason.Replaced);
                _buffs.Remove(existing);
                return CreateAndAdd(config, caster);
            case BuffOverlapMode.ExtendDuration:
                existing.ExtendDuration();
                return existing;
            case BuffOverlapMode.Stack:
                existing.AddStack();
                return existing;
            default:
                existing.ExtendDuration();
                return existing;
        }
    }

    public bool Dispel(int instanceId)
    {
        for (int i = 0; i < _buffs.Count; i++)
        {
            BuffBase buff = _buffs[i];
            if (buff.InstanceId != instanceId)
            {
                continue;
            }

            if (!buff.Config.Dispellable)
            {
                return false;
            }

            buff.Finish(BuffRemoveReason.Dispelled);
            _buffs.RemoveAt(i);
            return true;
        }

        return false;
    }

    public void CatchUpEmitter(EmitterBase emitter)
    {
        for (int i = 0; i < _buffs.Count; i++)
        {
            _buffs[i].CatchUpEmitter(emitter);
        }
    }

    public void Update(float dt)
    {
        for (int i = 0; i < _buffs.Count; i++)
        {
            _buffs[i].Update(dt);
        }

        for (int i = _buffs.Count - 1; i >= 0; i--)
        {
            if (_buffs[i].IsFinished)
            {
                _buffs.RemoveAt(i);
            }
        }
    }

    public void Clear()
    {
        for (int i = 0; i < _buffs.Count; i++)
        {
            _buffs[i].Finish(BuffRemoveReason.OwnerGone);
        }

        _buffs.Clear();
    }

    private BuffBase CreateAndAdd(buff config, Entity caster)
    {
        BuffBase buff = BuffFactory.Create(config);
        if (buff == null)
        {
            return null;
        }

        buff.Setup(_host, caster, config);
        _buffs.Add(buff);
        return buff;
    }

    private BuffBase FindLiving(int configId, Entity caster)
    {
        for (int i = 0; i < _buffs.Count; i++)
        {
            BuffBase buff = _buffs[i];
            if (buff.IsFinished || buff.Caster != caster || buff.Config == null || buff.Config.Id != configId)
            {
                continue;
            }

            return buff;
        }

        return null;
    }

    private static bool ContainsConfigId(int[] configIds, int configId)
    {
        for (int i = 0; i < configIds.Length; i++)
        {
            if (configIds[i] == configId)
            {
                return true;
            }
        }

        return false;
    }

    private BuffBase FindByName(string name)
    {
        for (int i = 0; i < _buffs.Count; i++)
        {
            if (!_buffs[i].IsFinished && _buffs[i].Name == name)
            {
                return _buffs[i];
            }
        }

        return null;
    }
}
