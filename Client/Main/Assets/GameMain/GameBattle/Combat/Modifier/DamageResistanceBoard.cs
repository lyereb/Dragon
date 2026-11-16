using System.Collections.Generic;
using cfg.combat.dragon;

/// <summary>
/// 按伤害类型累加的抗性。没有记录的类型是 0。合计限制在 0 到 1。
/// 枚举里新增的伤害类型不用再加字段，结算读到空值就是 0。
/// </summary>
public sealed class DamageResistanceBoard
{
    private struct Entry
    {
        public int Handle;
        public DamageType Type;
        public float Value;
    }

    private readonly List<Entry> _entries = new List<Entry>();
    private readonly List<int> _baseHandles = new List<int>();
    private int _nextHandle = 1;

    public void SetBase(IReadOnlyDictionary<DamageType, float> source)
    {
        for (int i = 0; i < _baseHandles.Count; i++)
        {
            Remove(_baseHandles[i]);
        }

        _baseHandles.Clear();
        if (source == null)
        {
            return;
        }

        foreach (KeyValuePair<DamageType, float> pair in source)
        {
            if (pair.Value == 0f)
            {
                continue;
            }

            _baseHandles.Add(Add(pair.Key, pair.Value));
        }
    }

    public int Add(DamageType type, float value)
    {
        int handle = _nextHandle++;
        _entries.Add(new Entry
        {
            Handle = handle,
            Type = type,
            Value = value,
        });
        return handle;
    }

    public void Remove(int handle)
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            if (_entries[i].Handle != handle)
            {
                continue;
            }

            _entries.RemoveAt(i);
            return;
        }
    }

    public float Resolve(DamageType type)
    {
        float sum = 0f;
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Type == type)
            {
                sum += _entries[i].Value;
            }
        }

        if (sum < 0f)
        {
            return 0f;
        }

        if (sum > 1f)
        {
            return 1f;
        }

        return sum;
    }
}
