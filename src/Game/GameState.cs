using RogueDefense.Core;
using System.Numerics;

namespace RogueDefense.Gameplay;

public sealed class GameState
{
    public Grid Grid { get; }
    public List<Enemy> Enemies { get; } = new();
    public List<Tower> Towers { get; } = new();
    public List<Projectile> Projectiles { get; } = new();
    public List<Particle> Particles { get; } = new();

    public Point Spawn { get; set; }
    public Point Goal { get; set; }
    public List<Point> CachedPath { get; private set; } = new();

    public int Gold { get; set; } = 80;
    public int Lives { get; set; } = 12;

    public int WaveNumber { get; set; } = 0;
    public int MaxWaves { get; set; } = 20;
    public bool Won { get; set; } = false;

    public float Time { get; private set; }

    public Point HoverCell { get; set; }
    public TowerType SelectedTower { get; set; } = TowerType.Archer;

    public readonly int TileSize;

    public GameState(int cols, int rows, int tileSize)
    {
        Grid = new Grid(cols, rows, tileSize);
        TileSize = tileSize;
        Spawn = new Point(1, rows / 2);
        Goal = new Point(cols - 2, rows / 2);
    }

    public void TickTime(float dt) => Time += dt;

    public void RecomputePath() => CachedPath = Pathfinding.AStar(Grid, Spawn, Goal);

    public bool PathExists() => CachedPath.Count > 0;

    public Vector2 CellToCenter(Point p) => new((p.X + 0.5f) * TileSize, (p.Y + 0.5f) * TileSize);
}
