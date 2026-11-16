using System.Collections.Generic;
using cfg.combat.dragon;
using UnityEngine;

public static class BulletFactory
{
    public static BulletBase Create(BulletSpawnArgs args)
    {
        if (args == null || args.Config == null || args.Params == null)
        {
            Debug.LogError("BulletFactory.Create fail, args or config is null.");
            return null;
        }

        BulletBase bullet;
        switch (args.Config.BulletType)
        {
            case BulletType.Melee:
                bullet = new MeleeBullet();
                break;
            case BulletType.Linear:
                bullet = new LinearBullet();
                break;
            case BulletType.Chain:
                bullet = new ChainBullet();
                break;
            case BulletType.Zone:
                bullet = new ZoneBullet();
                break;
            default:
                Debug.LogError($"BulletFactory.Create fail, unknown bullet type={args.Config.BulletType}, id={args.Config.Id}");
                return null;
        }

        bullet.Initialize(args);
        return bullet;
    }
}

/// <summary>
/// 全场子弹的唯一更新者。发射器只负责把子弹交进来。
/// </summary>
public sealed class BulletManager
{
    private readonly List<BulletBase> _bullets = new List<BulletBase>();
    private int _nextInstanceId = 1;

    public BulletManager()
    {
    }

    public void Spawn(BulletSpawnArgs args)
    {
        BulletBase bullet = BulletFactory.Create(args);
        if (bullet == null)
        {
            return;
        }

        bullet.AssignInstanceId(_nextInstanceId++);
        _bullets.Add(bullet);
        bullet.Arm();
    }

    public void Update(float dt)
    {
        int count = _bullets.Count;
        for (int i = 0; i < count; i++)
        {
            _bullets[i].Update(dt);
        }

        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            if (!_bullets[i].IsFinished)
            {
                continue;
            }

            _bullets[i].DisposeFSM();
            _bullets.RemoveAt(i);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < _bullets.Count; i++)
        {
            _bullets[i].Shutdown();
        }

        _bullets.Clear();
    }
}
