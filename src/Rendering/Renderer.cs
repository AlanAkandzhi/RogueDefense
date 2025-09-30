using Raylib_cs;
using RogueDefense.Gameplay;
using RogueDefense.Core;
using RogueDefense.Gameplay.Systems;
using System.Numerics;

namespace RogueDefense.Rendering;

public sealed class Renderer
{
    private readonly GameState _s;
    public Renderer(GameState s) { _s = s; }

    static readonly Color White = new Color(255, 255, 255, 255);

    static void DrawTextShadow(string text, int x, int y, int size, Color col)
    {
        var shadow = new Color(0, 0, 0, 120);
        Raylib.DrawText(text, x + 1, y + 1, size, shadow);
        Raylib.DrawText(text, x, y, size, col);
    }

    static void DrawTag(string text, int x, int y, int padX, int padY, int size, Color bg, Color fg)
    {
        int tw = Raylib.MeasureText(text, size);
        var rect = new Rectangle(x, y, tw + padX * 2, size + padY * 2);
        Raylib.DrawRectangleRounded(rect, 0.35f, 8, bg);
        DrawTextShadow(text, x + padX, y + padY, size, fg);
    }

    public void DrawWorld()
    {
        for (int x = 0; x < _s.Grid.Cols; x++)
        for (int y = 0; y < _s.Grid.Rows; y++)
        {
            var r = new Rectangle(x * _s.TileSize, y * _s.TileSize, _s.TileSize - 1, _s.TileSize - 1);
            var c = _s.Grid.IsBlocked(new(x, y)) ? new Color(128, 128, 128, 255) : new Color(245, 245, 245, 255);
            Raylib.DrawRectangleRec(r, c);
        }

        Raylib.DrawRectangle(_s.Spawn.X * _s.TileSize, _s.Spawn.Y * _s.TileSize, _s.TileSize - 1, _s.TileSize - 1, new Color(0, 200, 0, 255));
        Raylib.DrawRectangle(_s.Goal.X  * _s.TileSize, _s.Goal.Y  * _s.TileSize,  _s.TileSize - 1, _s.TileSize - 1,  new Color(220, 20, 60, 255));

        foreach (var p in _s.CachedPath)
            Raylib.DrawCircle((int)((p.X + 0.5f) * _s.TileSize), (int)((p.Y + 0.5f) * _s.TileSize), 4, new Color(0, 120, 255, 255));
    }

    public void DrawEntities()
    {
        foreach (var t in _s.Towers)
        {
            var center = _s.CellToCenter(t.Cell);
            Color col = t.Type switch
            {
                TowerType.Archer => new Color(60, 140, 60, 255),
                TowerType.Frost  => new Color(80, 130, 200, 255),
                TowerType.Cannon => new Color(130, 90, 60, 255),
                _ => new Color(230, 220, 200, 255)
            };
            Raylib.DrawCircle((int)center.X, (int)center.Y, _s.TileSize * 0.33f, col);
            Raylib.DrawCircleLines((int)center.X, (int)center.Y, t.EffectiveRange * _s.TileSize, new Color(0, 0, 0, 60));
            DrawTextShadow($"L{t.Level}", (int)center.X - 8, (int)center.Y - 30, 16, new Color(0, 0, 0, 255));
        }

        foreach (var e in _s.Enemies)
        {
            var hpPct = Math.Clamp(e.Hp / e.MaxHp, 0, 1);
            Color color = e.Type switch
            {
                EnemyType.Grunt     => new Color(128, 0, 0, 255),
                EnemyType.Runner    => new Color(200, 80, 80, 255),
                EnemyType.Tank      => new Color(100, 40, 40, 255),
                EnemyType.Splitter  => new Color(170, 30, 120, 255),
                _ => new Color(128, 0, 0, 255)
            };
            Raylib.DrawCircle((int)e.Pos.X, (int)e.Pos.Y, _s.TileSize * 0.28f, color);

            var barW = _s.TileSize * 0.6f;
            Raylib.DrawRectangle((int)(e.Pos.X - barW/2), (int)(e.Pos.Y - _s.TileSize * 0.45f), (int)barW, 4, new Color(0, 0, 0, 255));
            Raylib.DrawRectangle((int)(e.Pos.X - barW/2), (int)(e.Pos.Y - _s.TileSize * 0.45f), (int)(barW * hpPct), 4, new Color(0, 230, 0, 255));
        }

        foreach (var p in _s.Projectiles)
            Raylib.DrawCircle((int)p.Pos.X, (int)p.Pos.Y, p.SourceType == TowerType.Cannon ? 6 : 4, new Color(0, 0, 0, 255));

        foreach (var pr in _s.Particles)
        {
            float t = Math.Clamp(pr.Time / MathF.Max(pr.Lifetime, 0.0001f), 0f, 1f);
            byte a = (byte)(t * 255f);
            Raylib.DrawCircle((int)pr.Pos.X, (int)pr.Pos.Y, 2, new Color((byte)255, (byte)210, (byte)120, a));
        }
    }

    public void DrawHud()
    {
        int topH = 34;
        Raylib.DrawRectangle(0, 0, Game.WIDTH, topH, new Color(20, 20, 20, 170));

        int x = 10;
        int y = 8;
        DrawTextShadow($"Gold: {_s.Gold}",   x, y, 18, White); x += Raylib.MeasureText($"Gold: {_s.Gold}", 18) + 18;
        DrawTextShadow($"Lives: {_s.Lives}", x, y, 18, White); x += Raylib.MeasureText($"Lives: {_s.Lives}", 18) + 18;
        DrawTextShadow($"Wave: {_s.WaveNumber}/{_s.MaxWaves}", x, y, 18, White);

        string diff = WaveSystem.DIFFICULTY.ToString();
        string fpsText = $"{Raylib.GetFPS()} fps";
        int tagPadX = 10, tagPadY = 4, tagSize = 16;

        int fpsW = Raylib.MeasureText(fpsText, 16);
        int xRight = Game.WIDTH - 10 - fpsW;
        DrawTextShadow(fpsText, xRight, y + 1, 16, new Color(230,230,230,255));

        int tagW = Raylib.MeasureText(diff, tagSize) + tagPadX * 2;
        xRight -= (tagW + 12);
        var tagBg = diff switch
        {
            "Normal" => new Color(40, 145, 80, 240),
            "Hard"   => new Color(210, 140, 30, 240),
            _        => new Color(190, 40, 60, 240) 
        };
        DrawTag(diff, xRight, y - 2, tagPadX, tagPadY, tagSize, tagBg, White);

        var hc = _s.HoverCell;
        if (_s.Grid.InBounds(hc))
        {
            var r = new Rectangle(hc.X * _s.TileSize, hc.Y * _s.TileSize, _s.TileSize - 1, _s.TileSize - 1);
            Raylib.DrawRectangleLinesEx(r, 2, new Color(0, 100, 0, 255));
        }

        string help = "[1]Archer   [2]Frost   [3]Cannon    |    LMB Place   RMB Sell    |    U Upgrade    |    R Restart / M Menu";
        int helpSize = 18;
        int helpW = Raylib.MeasureText(help, helpSize);
        int barH = 32;
        Raylib.DrawRectangle(0, Game.HEIGHT - barH, Game.WIDTH, barH, new Color(20, 20, 20, 170));
        int hx = (Game.WIDTH - helpW) / 2;
        int hy = Game.HEIGHT - barH + 6;
        DrawTextShadow(help, hx, hy, helpSize, White);

        if (_s.Lives <= 0)
        {
            string msg = "Defeat!";
            int w = Raylib.MeasureText(msg, 36);
            DrawTextShadow(msg, (Game.WIDTH - w) / 2, Game.HEIGHT / 2 - 18, 36, White);
        }
        if (_s.Won)
        {
            string msg = "Victory!";
            int w = Raylib.MeasureText(msg, 36);
            DrawTextShadow(msg, (Game.WIDTH - w) / 2, Game.HEIGHT / 2 - 18, 36, White);
        }
    }
}
