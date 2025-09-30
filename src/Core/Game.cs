using Raylib_cs;
using RogueDefense.Gameplay;
using RogueDefense.Gameplay.Systems;
using RogueDefense.Rendering;

namespace RogueDefense.Core;

public static class Game
{
    public const int TILE = 32;
    public const int COLS = 28;
    public const int ROWS = 18;
    public const int WIDTH = COLS * TILE;
    public const int HEIGHT = ROWS * TILE;

    enum Screen { Menu, Playing, GameOver, Victory }
    static Screen _screen = Screen.Menu;

    static (GameState state, Renderer renderer, Ui ui, PlacementSystem placement, WaveSystem waves, CombatSystem combat) MakeWorld()
    {
        var s = new GameState(COLS, ROWS, TILE);
        var r = new Renderer(s);
        var ui = new Ui(s);
        var placement = new PlacementSystem(s);
        var waves = new WaveSystem(s);
        var combat = new CombatSystem(s);

        s.Grid.GenerateArenaBorders();
        s.Grid.GenerateRandomObstacles(0.06f);
        s.Spawn = s.Grid.RandomEdgeCell(left: true);
        s.Goal  = s.Grid.RandomEdgeCell(left: false);
        s.RecomputePath();

        return (s, r, ui, placement, waves, combat);
    }

    public static void Run()
    {
        Raylib.InitWindow(WIDTH, HEIGHT, "Rogue Defense (C# + raylib-cs)");
        Raylib.SetTargetFPS(60);

        var world = MakeWorld();

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(80, 80, 80, 255));

            switch (_screen)
            {
                case Screen.Menu:
                {
                    string title = "ROGUE DEFENSE";
                    int tw = Raylib.MeasureText(title, 40);
                    Raylib.DrawText(title, (WIDTH - tw) / 2, 48, 40, new Color(0, 0, 0, 255));

                    Raylib.DrawText("Choose difficulty:", 40, 140, 22, new Color(0,0,0,255));
                    Raylib.DrawText("[1] Normal",         60, 175, 20, new Color(0,0,0,255));
                    Raylib.DrawText("[2] Hard",           60, 200, 20, new Color(0,0,0,255));
                    Raylib.DrawText("[3] Brutal",         60, 225, 20, new Color(0,0,0,255));

                    Raylib.DrawText("Controls: 1/2/3 = tower type, LMB=Place, RMB=Sell, U=Upgrade, R=Restart, M=Menu", 40, 270, 18, new Color(0,0,0,255));

                    if (Raylib.IsKeyPressed((KeyboardKey)49)) { WaveSystem.DIFFICULTY = WaveSystem.Difficulty.Normal; world = MakeWorld(); _screen = Screen.Playing; }
                    if (Raylib.IsKeyPressed((KeyboardKey)50)) { WaveSystem.DIFFICULTY = WaveSystem.Difficulty.Hard;   world = MakeWorld(); _screen = Screen.Playing; }
                    if (Raylib.IsKeyPressed((KeyboardKey)51)) { WaveSystem.DIFFICULTY = WaveSystem.Difficulty.Brutal; world = MakeWorld(); _screen = Screen.Playing; }
                    break;
                }

                case Screen.Playing:
                {
                    if (Raylib.IsKeyPressed((KeyboardKey)82))
                    { world = MakeWorld(); }

                    if (Raylib.IsKeyPressed((KeyboardKey)77))
                    { _screen = Screen.Menu; break; }

                    Input.Update();
                    world.ui.Update(dt);
                    world.placement.Update(dt);
                    world.waves.Update(dt);
                    world.combat.Update(dt);

                    foreach (var e in world.state.Enemies) e.Update(dt, world.state);

                    world.renderer.DrawWorld();
                    world.renderer.DrawEntities();
                    world.renderer.DrawHud();

                    if (world.state.Lives <= 0)
                        _screen = Screen.GameOver;
                    else if (world.state.Won)
                        _screen = Screen.Victory;

                    break;
                }

                case Screen.GameOver:
                {
                    world.renderer.DrawWorld();
                    world.renderer.DrawEntities();
                    world.renderer.DrawHud();

                    string msg = "Defeat! Press R to restart  |  M for Menu";
                    int w = Raylib.MeasureText(msg, 28);
                    Raylib.DrawText(msg, (WIDTH - w) / 2, HEIGHT / 2 - 14, 28, new Color(0, 0, 0, 255));

                    if (Raylib.IsKeyPressed((KeyboardKey)82))
                    { world = MakeWorld(); _screen = Screen.Playing; }

                    if (Raylib.IsKeyPressed((KeyboardKey)77))
                    { _screen = Screen.Menu; }

                    break;
                }

                case Screen.Victory:
                {
                    world.renderer.DrawWorld();
                    world.renderer.DrawEntities();
                    world.renderer.DrawHud();

                    string msg = "Victory! Press R to play again  |  M for Menu";
                    int w = Raylib.MeasureText(msg, 28);
                    Raylib.DrawText(msg, (WIDTH - w) / 2, HEIGHT / 2 - 14, 28, new Color(0, 0, 0, 255));

                    if (Raylib.IsKeyPressed((KeyboardKey)82))
                    { world = MakeWorld(); _screen = Screen.Playing; }

                    if (Raylib.IsKeyPressed((KeyboardKey)77))
                    { _screen = Screen.Menu; }

                    break;
                }
            }

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
