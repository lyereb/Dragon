using System.Collections.Generic;
using cfg.combat.dragon;

public partial class Entity
{
    private static int _nextInstanceId = 1;

    private BuffManager _buffManager;
    private readonly List<EmitterBase> _emitters = new List<EmitterBase>();
    private readonly List<int> _initialEmitterIds = new List<int>();
    private int _moveLock;
    private int _attackLock;
    private int _invulnerable;

    public int InstanceId { get; private set; }
    public int FactionId { get; private set; }
    public bool IsDead { get; protected set; }
    public bool IsDespawnPending { get; private set; }
    public float CurrentHP { get; protected set; }
    public float CurrentSP { get; protected set; }
    public BattleCombat Battle { get; private set; }
    public CombatModifierBoard CombatModifiers { get; private set; }
    /// <summary>按伤害类型的抗性，含治疗。默认 0，结算时伤害或治疗乘 (1 - 抗性)。</summary>
    public DamageResistanceBoard Resistances { get; } = new DamageResistanceBoard();
    public BuffManager Buffs => _buffManager;
    public IReadOnlyList<EmitterBase> Emitters => _emitters;

    public BuffBase AddBuff(int buffConfigId, Entity caster)
    {
        if (_buffManager == null)
        {
            return null;
        }

        return _buffManager.Add(buffConfigId, caster);
    }

    public EmitterBase AddEmitter(int emitterConfigId)
    {
        EmitterBase emitter = EmitterFactory.Create(this, emitterConfigId, Battle);
        if (emitter == null)
        {
            return null;
        }

        _emitters.Add(emitter);
        return emitter;
    }

    protected void MarkDespawn()
    {
        IsDespawnPending = true;
    }

    public bool BlocksMovement()
    {
        return _moveLock > 0;
    }

    public bool BlocksAttack()
    {
        return _attackLock > 0;
    }

    public bool IsInvulnerable()
    {
        return _invulnerable > 0;
    }

    public float GetResistance(DamageType damageType)
    {
        return Resistances != null ? Resistances.Resolve(damageType) : 0f;
    }

    public void AddBuffRule(BuffRule rule)
    {
        switch (rule)
        {
            case BuffRule.Stun:
                _moveLock++;
                _attackLock++;
                break;
            case BuffRule.Disarm:
                _attackLock++;
                break;
            case BuffRule.Invulnerable:
                _invulnerable++;
                break;
        }
    }

    public void RemoveBuffRule(BuffRule rule)
    {
        switch (rule)
        {
            case BuffRule.Stun:
                _moveLock = DecrementLock(_moveLock);
                _attackLock = DecrementLock(_attackLock);
                break;
            case BuffRule.Disarm:
                _attackLock = DecrementLock(_attackLock);
                break;
            case BuffRule.Invulnerable:
                _invulnerable = DecrementLock(_invulnerable);
                break;
        }
    }

    public void NotifyAttackInput()
    {
        if (BlocksAttack())
        {
            return;
        }

        for (int i = 0; i < _emitters.Count; i++)
        {
            _emitters[i].NotifyAttackInput();
        }
    }

    /// <summary>
    /// 角色表加上 emitterIds 之后，在子类里把 id 填进 result。
    /// </summary>
    protected virtual void CollectInitialEmitterConfigIds(List<int> result)
    {
    }

    protected virtual int GetDefaultFactionId()
    {
        return CombatFaction.Neutral;
    }

    protected void BindBattleCombat()
    {
        FactionId = GetDefaultFactionId();
        CombatModifiers = new CombatModifierBoard();
        _buffManager = new BuffManager(this);
        Battle = Dragon.Logic.MapSceneManager.Instance != null
            ? Dragon.Logic.MapSceneManager.Instance.BattleCombat
            : null;
        Battle?.AttachEntity(this);

        _initialEmitterIds.Clear();
        CollectInitialEmitterConfigIds(_initialEmitterIds);
        for (int i = 0; i < _initialEmitterIds.Count; i++)
        {
            AddEmitter(_initialEmitterIds[i]);
        }
    }

    protected void UpdateBuffs(float dt)
    {
        _buffManager?.Update(dt);
    }

    protected void UpdateEmitters(float dt)
    {
        for (int i = 0; i < _emitters.Count; i++)
        {
            _emitters[i].Update(dt);
        }

        for (int i = _emitters.Count - 1; i >= 0; i--)
        {
            if (!_emitters[i].IsShutdown)
            {
                continue;
            }

            _emitters[i].DisposeFSM();
            _emitters.RemoveAt(i);
        }
    }

    private static int DecrementLock(int count)
    {
        return count > 0 ? count - 1 : 0;
    }

    private void ShutdownBattleCombat()
    {
        for (int i = 0; i < _emitters.Count; i++)
        {
            EmitterBase emitter = _emitters[i];
            emitter.Shutdown();
            emitter.DisposeFSM();
        }

        _emitters.Clear();
        _buffManager?.Clear();
        Battle?.DetachEntity(this);
        Battle = null;
    }
}
