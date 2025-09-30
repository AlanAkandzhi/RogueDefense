using Raylib_cs;
using RogueDefense.Core;

namespace RogueDefense.Gameplay.Systems;

public sealed class PlacementSystem
{
    private readonly GameState _s;

    public PlacementSystem(GameState s) { _s = s; }

    public void Update(float dt)
    {
        _s.TickTime(dt);

        if (Input.KeyPressed((KeyboardKey)49)) _s.SelectedTower = TowerType.Archer;
        if (Input.KeyPressed((KeyboardKey)50)) _s.SelectedTower = TowerType.Frost;
        if (Input.KeyPressed((KeyboardKey)51)) _s.SelectedTower = TowerType.Cannon;

        var m = Raylib.GetMousePosition();
        var c = new Point((int)(m.X / _s.TileSize), (int)(m.Y / _s.TileSize));
        _s.HoverCell = c;

        if (Input.LeftPressed)  TryPlace(c);
        if (Input.RightPressed) TrySell(c);

        if (Raylib.IsKeyPressed((KeyboardKey)85))
        {
            var t = _s.Towers.FirstOrDefault(t => t.Cell.Equals(c));
            if (t != null)
            {
                int cost = UpgradeCost(t.Level);
                if (_s.Gold >= cost && t.Level < 5)
                {
                    _s.Gold -= cost;
                    t.Upgrade();
                }
            }
        }
    }

    int Cost(TowerType t) => t switch
{
    TowerType.Archer => 40,
    TowerType.Frost  => 55,
    TowerType.Cannon => 70,
    _ => 40
};

int UpgradeCost(int lvl) => 25 + lvl * 35;

    bool IsOccupied(Point c) => _s.Towers.Any(t => t.Cell.Equals(c));

    void TryPlace(Point cell)
    {
        if (!_s.Grid.InBounds(cell) || _s.Grid.IsBlocked(cell) || IsOccupied(cell)) return;

        int cost = Cost(_s.SelectedTower);
        if (_s.Gold < cost) return;

        _s.Grid.SetBlocked(cell, true);
        var newPath = Pathfinding.AStar(_s.Grid, _s.Spawn, _s.Goal);
        if (newPath.Count == 0) { _s.Grid.SetBlocked(cell, false); return; }

        _s.Gold -= cost;
        _s.Towers.Add(new Tower(_s.SelectedTower, cell));

        _s.RecomputePath();
        foreach (var e in _s.Enemies)
        {
            e.Path = new List<Point>(_s.CachedPath);
            e.PathIndex = Math.Min(e.PathIndex, Math.Max(0, e.Path.Count - 1));
        }
    }

    void TrySell(Point cell)
    {
        var t = _s.Towers.FirstOrDefault(t => t.Cell.Equals(cell));
        if (t == null) return;

        _s.Towers.Remove(t);
        _s.Grid.SetBlocked(cell, false);
        _s.RecomputePath();
        _s.Gold += Cost(t.Type) / 2;
    }
}
