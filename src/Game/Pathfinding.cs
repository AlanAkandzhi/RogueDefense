using System.Collections.Generic;

namespace RogueDefense.Gameplay;

public static class Pathfinding
{
    public static List<Point> AStar(Grid grid, Point s, Point g)
    {
        var open = new PriorityQueue<Point, int>();
        var came = new Dictionary<Point, Point>();
        var gScore = new Dictionary<Point, int> { [s] = 0 };

        open.Enqueue(s, 0);

        int Heur(Point a, Point b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

        while (open.Count > 0)
        {
            var current = open.Dequeue();
            if (current.Equals(g)) break;

            foreach (var nb in grid.Neighbors4(current))
            {
                int tentative = gScore[current] + 1;
                if (!gScore.TryGetValue(nb, out var best) || tentative < best)
                {
                    gScore[nb] = tentative;
                    came[nb] = current;
                    int f = tentative + Heur(nb, g);
                    open.Enqueue(nb, f);
                }
            }
        }

        if (!came.ContainsKey(g)) return new();
        var path = new List<Point>();
        var cur = g;
        while (!cur.Equals(s))
        {
            path.Add(cur);
            cur = came[cur];
        }
        path.Reverse();
        return path;
    }
}
