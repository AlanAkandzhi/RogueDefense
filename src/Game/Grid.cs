using RogueDefense.Core;

namespace RogueDefense.Gameplay;

public readonly record struct Point(int X, int Y)
{
    public static readonly Point Zero = new(0, 0);
}

public sealed class Grid
{
    public int Cols { get; }
    public int Rows { get; }
    public int TileSize { get; }
    private readonly bool[,] _blocked;

    public Grid(int cols, int rows, int tileSize)
    {
        Cols = cols; Rows = rows; TileSize = tileSize;
        _blocked = new bool[Cols, Rows];
    }

    public bool InBounds(Point p) => p.X >= 0 && p.X < Cols && p.Y >= 0 && p.Y < Rows;
    public bool IsBlocked(Point p) => _blocked[p.X, p.Y];
    public void SetBlocked(Point p, bool v) => _blocked[p.X, p.Y] = v;

    public IEnumerable<Point> Neighbors4(Point p)
    {
        Point[] dirs = { new(p.X+1,p.Y), new(p.X-1,p.Y), new(p.X,p.Y+1), new(p.X,p.Y-1) };
        foreach (var d in dirs) if (InBounds(d) && !IsBlocked(d)) yield return d;
    }

    public void GenerateArenaBorders()
    {
        for (int x = 0; x < Cols; x++) { SetBlocked(new(x, 0), true); SetBlocked(new(x, Rows-1), true); }
        for (int y = 0; y < Rows; y++) { SetBlocked(new(0, y), true); SetBlocked(new(Cols-1, y), true); }
    }

    public void GenerateRandomObstacles(float fill = 0.05f)
    {
        for (int x = 2; x < Cols-2; x++)
            for (int y = 2; y < Rows-2; y++)
                if (Rand.Chance(fill)) SetBlocked(new(x,y), true);
    }

    public Point RandomEdgeCell(bool left)
    {
        int x = left ? 1 : Cols - 2;
        int y;
        int tries = 0;
        do { y = Rand.Range(1, Rows - 2); tries++; } while (IsBlocked(new(x, y)) && tries < 500);
        SetBlocked(new(x, y), false);
        return new(x, y);
    }
}
