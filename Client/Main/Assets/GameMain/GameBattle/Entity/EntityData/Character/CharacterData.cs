using System;
using System.Collections.Generic;
using cfg.entity.dragon;

/// <summary>
/// 属性修饰：固定值与百分比在最终公式中分别累加。
/// 最终 = (基础 + ΣFlat) * (1 + ΣPercent)；Percent 为小数，0.2 即 +20%。
/// </summary>
public enum CharacterAttrModifierKind
{
    Flat,
    Percent,
}

public class CharacterData : EntityData
{
    private struct ModifierEntry
    {
        public CharacterAttr Attr;
        public CharacterAttrModifierKind Kind;
        public float Value;
    }

    public Dictionary<CharacterAttr, float> BaseAttributes { get; private set; }

    private readonly Dictionary<int, ModifierEntry> _modifiers = new Dictionary<int, ModifierEntry>();
    private int _nextModifierId = 1;
    private readonly Dictionary<CharacterAttr, float> _currentAttributes = new Dictionary<CharacterAttr, float>();

    public CharacterData()
    {
        BaseAttributes = new Dictionary<CharacterAttr, float>();
        RebuildCurrentAttributeCache();
    }

    public void ApplyAttributesFromConfig(IReadOnlyDictionary<CharacterAttr, float> source)
    {
        BaseAttributes.Clear();
        if (source != null)
        {
            foreach (KeyValuePair<CharacterAttr, float> pair in source)
                BaseAttributes[pair.Key] = pair.Value;
        }
        RebuildCurrentAttributeCache();
    }

    public float GetBaseAttribute(CharacterAttr attr)
    {
        return BaseAttributes != null && BaseAttributes.TryGetValue(attr, out float value) ? value : 0f;
    }

    /// <summary>
    /// 添加修饰，返回句柄供 <see cref="RemoveModifier"/> 移除。
    /// </summary>
    public int AddModifier(CharacterAttr attr, CharacterAttrModifierKind kind, float value)
    {
        int id = _nextModifierId++;
        _modifiers[id] = new ModifierEntry { Attr = attr, Kind = kind, Value = value };
        RefreshCachedAttribute(attr);
        return id;
    }

    public bool RemoveModifier(int modifierId)
    {
        if (!_modifiers.TryGetValue(modifierId, out ModifierEntry entry))
            return false;
        _modifiers.Remove(modifierId);
        RefreshCachedAttribute(entry.Attr);
        return true;
    }

    /// <summary>
    /// 当前属性：(基础值 + 所有固定修饰之和) * (1 + 所有百分比修饰之和)。
    /// </summary>
    public float GetCurrentAttribute(CharacterAttr attr)
    {
        return _currentAttributes[attr];
    }

    private void RebuildCurrentAttributeCache()
    {
        _currentAttributes.Clear();
        foreach (CharacterAttr attr in Enum.GetValues(typeof(CharacterAttr)))
            _currentAttributes[attr] = ComputeCurrentAttribute(attr);
    }

    private void RefreshCachedAttribute(CharacterAttr attr)
    {
        _currentAttributes[attr] = ComputeCurrentAttribute(attr);
    }

    private float ComputeCurrentAttribute(CharacterAttr attr)
    {
        float baseVal = GetBaseAttribute(attr);
        float sumFlat = 0f;
        float sumPercent = 0f;
        foreach (ModifierEntry m in _modifiers.Values)
        {
            if (m.Attr != attr)
                continue;
            if (m.Kind == CharacterAttrModifierKind.Flat)
                sumFlat += m.Value;
            else
                sumPercent += m.Value;
        }
        return (baseVal + sumFlat) * (1f + sumPercent);
    }
}
