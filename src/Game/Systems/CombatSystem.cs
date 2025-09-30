using System.Numerics;
using RogueDefense.Core;

namespace RogueDefense.Gameplay.Systems;

public sealed class CombatSystem
{
    private readonly GameState _s;
    public CombatSystem(GameState s) { _s = s; }

    public void Update(float dt)
    {
        foreach (var t in _s.Towers)
            t.Update(dt, _s);

        for (int i = _s.Projectiles.Count - 1; i >= 0; i--)
        {
            var p = _s.Projectiles[i];
            p.Update(dt);

            if (p.Life <= 0f)
            {
                _s.Projectiles.RemoveAt(i);
                continue;
            }

            Enemy? hit = null;

            if (p.Target != null && p.Target.Hp > 0)
            {
                if (Vector2.DistanceSquared(p.Pos, p.Target.Pos) <= p.HitRadius * p.HitRadius)
                    hit = p.Target;
            }

            if (hit == null)
            {
                foreach (var e in _s.Enemies)
                {
                    if (e.Hp <= 0) continue;
                    if (Vector2.DistanceSquared(p.Pos, e.Pos) <= p.HitRadius * p.HitRadius)
                    {
                        hit = e;
                        break;
                    }
                }
            }

            if (hit != null)
            {
                ApplyFrom(p.SourceType, p.Damage, hit);
                SpawnHitSparks(hit.Pos, 5);

                if (p.SourceType == TowerType.Cannon)
                {
                    float r = _s.TileSize * 0.9f;
                    float r2 = r * r;
                    foreach (var e in _s.Enemies)
                    {
                        if (e == hit || e.Hp <= 0) continue;
                        if (Vector2.DistanceSquared(e.Pos, hit.Pos) <= r2)
                        {
                            ApplyFrom(p.SourceType, p.Damage * 0.5f, e);
                            SpawnHitSparks(e.Pos, 3);
                        }
                    }
                }

                _s.Projectiles.RemoveAt(i);
            }
        }

        for (int i = _s.Enemies.Count - 1; i >= 0; i--)
        {
            var dead = _s.Enemies[i];
            if (dead.Hp <= 0)
            {
                _s.Gold += 1 + (int)MathF.Ceiling(dead.MaxHp / 30f);

                if (dead.Type == EnemyType.Splitter)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        var spawnCell = new Point(
                            Math.Clamp((int)(dead.Pos.X / _s.TileSize), 1, _s.Grid.Cols - 2),
                            Math.Clamp((int)(dead.Pos.Y / _s.TileSize), 1, _s.Grid.Rows - 2)
                        );
                        var g = new Enemy(EnemyType.Grunt, spawnCell, _s.TileSize)
                        {
                            Path = new List<Point>(_s.CachedPath),
                            PathIndex = Math.Min(dead.PathIndex, Math.Max(0, _s.CachedPath.Count - 1)),
                            Hp = 26, MaxHp = 26
                        };
                        _s.Enemies.Add(g);
                        SpawnHitSparks(g.Pos, 4);
                    }
                }

                _s.Enemies.RemoveAt(i);
            }
        }

        for (int i = _s.Particles.Count - 1; i >= 0; i--)
        {
            var pr = _s.Particles[i];
            pr.Update(dt);
            if (pr.Time <= 0) _s.Particles.RemoveAt(i);
        }
    }

    void ApplyFrom(TowerType src, float dmg, Enemy e)
    {
        e.ApplyDamage(dmg);
        switch (src)
        {
            case TowerType.Frost:
                e.AddEffect(new StatusEffect(StatusKind.Slow, duration: 1.2f, magnitude: 0.75f));
                break;
            case TowerType.Archer:
                if (Rand.Chance(0.10f))
                    e.AddEffect(new StatusEffect(StatusKind.Burn, duration: 1.8f, magnitude: 3.0f));
                break;
        }
    }

    void SpawnHitSparks(Vector2 pos, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var v = new Vector2(Rand.Range(-60, 60), Rand.Range(-60, 60));
            _s.Particles.Add(new Particle { Pos = pos, Vel = v, Time = Rand.Range(0.2f, 0.5f) });
        }
    }
}
