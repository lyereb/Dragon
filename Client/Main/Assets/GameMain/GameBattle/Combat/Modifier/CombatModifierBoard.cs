using System.Collections.Generic;
using cfg.combat.dragon;

public sealed class CombatModifierSpec
{
    public int SourceId;
    public CombatModifierScope Scope;
    public bool UseCharacterAttr;
    public cfg.entity.dragon.CharacterAttr CharacterAttr;
    public CombatParam Param;
    public CombatModifierOp Op;
    public float Value;

    public CombatModifierSpec Clone()
    {
        return new CombatModifierSpec
        {
            SourceId = SourceId,
            Scope = Scope,
            UseCharacterAttr = UseCharacterAttr,
            CharacterAttr = CharacterAttr,
            Param = Param,
            Op = Op,
            Value = Value,
        };
    }

    public static CombatModifierSpec FromConfig(modifier config, int sourceId, float multiplier)
    {
        if (config == null)
        {
            return null;
        }

        float scale = multiplier <= 0f ? 1f : multiplier;
        return new CombatModifierSpec
        {
            SourceId = sourceId,
            Scope = config.Scope,
            UseCharacterAttr = config.CharacterAttrId > 0,
            CharacterAttr = (cfg.entity.dragon.CharacterAttr)config.CharacterAttrId,
            Param = config.Param,
            Op = config.Op,
            Value = config.Value * scale,
        };
    }
}

/// <summary>
/// 挂在实体、发射器或子弹上的参数修改板。
/// 最终值 = (基础值 + 全部固定值) * (1 + 全部百分比)。百分比用小数，0.2 表示 +20%。
/// </summary>
public sealed class CombatModifierBoard
{
    private struct Entry
    {
        public int Handle;
        public int SourceId;
        public CombatParam Param;
        public CombatModifierOp Op;
        public float Value;
    }

    private readonly List<Entry> _entries = new List<Entry>();
    private int _nextHandle = 1;

    public int Add(CombatModifierSpec spec)
    {
        if (spec == null || spec.UseCharacterAttr || spec.Param == CombatParam.None)
        {
            return 0;
        }

        int handle = _nextHandle++;
        _entries.Add(new Entry
        {
            Handle = handle,
            SourceId = spec.SourceId,
            Param = spec.Param,
            Op = spec.Op,
            Value = spec.Value,
        });
        return handle;
    }

    public bool Remove(int handle)
    {
        if (handle <= 0)
        {
            return false;
        }

        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Handle != handle)
            {
                continue;
            }

            _entries.RemoveAt(i);
            return true;
        }

        return false;
    }

    public void RemoveBySource(int sourceId)
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            if (_entries[i].SourceId == sourceId)
            {
                _entries.RemoveAt(i);
            }
        }
    }

    public bool Has(CombatParam param)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Param == param)
            {
                return true;
            }
        }

        return false;
    }

    public float Resolve(CombatParam param, float baseValue)
    {
        return CombatModifierMath.Apply(baseValue, SumFlat(param), SumPercent(param));
    }

    public void Accumulate(CombatParam param, ref float flat, ref float percent)
    {
        flat += SumFlat(param);
        percent += SumPercent(param);
    }

    public void Clear()
    {
        _entries.Clear();
    }

    private float SumFlat(CombatParam param)
    {
        float sum = 0f;
        for (int i = 0; i < _entries.Count; i++)
        {
            Entry entry = _entries[i];
            if (entry.Param == param && entry.Op == CombatModifierOp.Flat)
            {
                sum += entry.Value;
            }
        }

        return sum;
    }

    private float SumPercent(CombatParam param)
    {
        float sum = 0f;
        for (int i = 0; i < _entries.Count; i++)
        {
            Entry entry = _entries[i];
            if (entry.Param == param && entry.Op == CombatModifierOp.Percent)
            {
                sum += entry.Value;
            }
        }

        return sum;
    }
}

public static class CombatModifierMath
{
    public static float Apply(float baseValue, float flat, float percent)
    {
        return (baseValue + flat) * (1f + percent);
    }

    public static float Combine(float baseValue, CombatParam param, CombatModifierBoard first, CombatModifierBoard second)
    {
        float flat = 0f;
        float percent = 0f;
        first?.Accumulate(param, ref flat, ref percent);
        second?.Accumulate(param, ref flat, ref percent);
        return Apply(baseValue, flat, percent);
    }
}
