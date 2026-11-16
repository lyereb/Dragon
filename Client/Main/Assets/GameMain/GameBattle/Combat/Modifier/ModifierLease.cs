using System;
using System.Collections.Generic;

/// <summary>
/// 一组修改器的撤销表。Buff 结束或局内奖励移除时，按登记的逆操作卸掉。
/// </summary>
public sealed class ModifierLease
{
    private readonly List<Action> _undos = new List<Action>();

    public void Track(Action undo)
    {
        if (undo != null)
        {
            _undos.Add(undo);
        }
    }

    public void Release()
    {
        for (int i = _undos.Count - 1; i >= 0; i--)
        {
            _undos[i]?.Invoke();
        }

        _undos.Clear();
    }
}
