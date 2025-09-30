using System.Numerics;

namespace RogueDefense.Gameplay;

public enum EnemyType { Grunt, Runner, Tank, Splitter }
public enum TowerType { Archer, Frost, Cannon }

public sealed class Enemy
{
    public EnemyType Type;
    public Vector2 Pos;
    public float Speed;
    public float Hp, MaxHp;
    public int PathIndex;
    public List<Point> Path = new();
    public readonly List<StatusEffect> Effects = new();

    public Enemy(EnemyType type, Point cell, float tileSize)
    {
        Type = type;
        PathIndex = 0;
        Pos = new Vector2((cell.X + 0.5f) * tileSize, (cell.Y + 0.5f) * tileSize);
        switch (type)
        {
             case EnemyType.Grunt:    MaxHp = Hp = 48; Speed = 3.6f; break;
             case EnemyType.Runner:   MaxHp = Hp = 30; Speed = 5.6f; break;
            case EnemyType.Tank:     MaxHp = Hp = 120; Speed = 2.6f; break;
             case EnemyType.Splitter: MaxHp = Hp = 62; Speed = 3.4f; break;
        }
    }

    public Point Cell(float tile) => new((int)(Pos.X / tile), (int)(Pos.Y / tile));

    public float CurrentMoveSpeed()
    {
        float s = Speed;
        foreach (var e in Effects)
            if (e.Kind == StatusKind.Slow) s *= e.Magnitude;
        return Math.Max(0.5f, s);
    }

    public void ApplyDamage(float dmg) => Hp -= dmg;

    public void AddEffect(StatusEffect eff)
    {
        var existing = Effects.FirstOrDefault(e => e.Kind == eff.Kind);
        if (existing != null)
        {
            existing.Duration = Math.Max(existing.Duration, eff.Duration);
            existing.Magnitude = Math.Min(existing.Magnitude, eff.Magnitude);
        }
        else Effects.Add(eff);
    }

    public void Update(float dt, GameState state)
    {
        for (int i = Effects.Count - 1; i >= 0; i--)
        {
            var e = Effects[i];
            if (e.Kind == StatusKind.Burn) Hp -= e.Magnitude * dt;
            e.Duration -= dt;
            if (e.Duration <= 0) Effects.RemoveAt(i);
        }

        if (PathIndex >= Path.Count) return;
        var tile = state.TileSize;
        var targetCell = Path[PathIndex];
        var targetPos = state.CellToCenter(targetCell);
        var dir = Vector2.Normalize(targetPos - Pos);
        if (float.IsNaN(dir.X) || float.IsNaN(dir.Y)) dir = Vector2.Zero;

        float speedPx = CurrentMoveSpeed() * tile;
        Pos += dir * speedPx * dt;

        if (Vector2.DistanceSquared(Pos, targetPos) < 4f) PathIndex++;

        if (PathIndex >= Path.Count)
        {
            state.Lives -= (int)MathF.Ceiling(MaxHp / 25f);
            Hp = -999;
        }
    }
}

public sealed class Tower
{
    public TowerType Type;
    public Point Cell;
    public float Range;
    public float Cooldown;
    private float _cd;

    public int Level = 1;
    public int MultiShot = 0;
    public float RangeMult = 1f;
    public float CooldownMult = 1f;

    public Tower(TowerType type, Point cell)
    {
        Type = type;
        Cell = cell;
        switch (type)
        {
            case TowerType.Archer: Range = 5.5f; Cooldown = 0.45f; break;
            case TowerType.Frost:  Range = 4.5f; Cooldown = 0.8f;  break;
            case TowerType.Cannon: Range = 4.0f; Cooldown = 0.9f;  break;
        }
    }

    public float EffectiveRange => Range * RangeMult;
    public float EffectiveCooldown => MathF.Max(0.08f, Cooldown * CooldownMult);

    public void Upgrade()
    {
        Level++;
        RangeMult *= 1.10f;
        CooldownMult *= 0.90f;
        if (Level == 3 || Level == 5) MultiShot++; 
    }

    public void Update(float dt, GameState s)
    {
        _cd -= dt;
        if (_cd > 0) return;

        var target = AcquireTarget(s);
        if (target is null) return;

        int shots = 1 + MultiShot;
        for (int i = 0; i < shots; i++)
        {
            var proj = Projectile.Create(Type, s.CellToCenter(Cell), target);
            if (i > 0) proj.AddRandomSpread(0.12f);
            s.Projectiles.Add(proj);
        }
        _cd = EffectiveCooldown;
    }

    Enemy? AcquireTarget(GameState s)
    {
        float best = float.MaxValue;
        Enemy? pick = null;
        foreach (var e in s.Enemies)
        {
            if (e.Hp <= 0) continue;
            float dCells = Vector2.Distance(s.CellToCenter(Cell), e.Pos) / s.TileSize;
            if (dCells <= EffectiveRange)
            {
                float key = -e.PathIndex;
                if (key < best) { best = key; pick = e; }
            }
        }
        return pick;
    }
}

public sealed class Projectile
{
    public Vector2 Pos;
    public Vector2 Vel;
    public float Radius = 5f;
    public float Damage;
    public Enemy? Target;
    public TowerType SourceType;

    public float Life = 3.5f;
    public float HitRadius = 18f;

    private static readonly Random _rng = new();

    public Projectile(Vector2 pos, Vector2 vel, float dmg, TowerType src, Enemy? target)
    {
        Pos = pos; Vel = vel; Damage = dmg; SourceType = src; Target = target;
    }

    public static Projectile Create(TowerType type, Vector2 from, Enemy target)
    {
        return type switch
        {
            TowerType.Archer => Ballistic(from, target, 420f, 14f, type),
            TowerType.Frost  => Ballistic(from, target, 360f, 8f,  type),
            TowerType.Cannon => Ballistic(from, target, 300f, 24f, type),
            _ => Ballistic(from, target, 400f, 10f, type)
        };
    }

    public void AddRandomSpread(float radians)
    {
        float ang = ((float)_rng.NextDouble() - 0.5f) * 2f * radians;
        float cos = MathF.Cos(ang);
        float sin = MathF.Sin(ang);
        Vel = new Vector2(Vel.X * cos - Vel.Y * sin, Vel.X * sin + Vel.Y * cos);
    }

    private static Projectile Ballistic(Vector2 from, Enemy target, float speed, float dmg, TowerType src)
    {
        var dir = target.Pos - from;
        if (dir.LengthSquared() < 1) dir = new(1, 0);
        dir = Vector2.Normalize(dir) * speed;
        return new Projectile(from, dir, dmg, src, target);
    }

    public void Update(float dt)
    {
        Pos += Vel * dt;
        Life -= dt;
    }
}
