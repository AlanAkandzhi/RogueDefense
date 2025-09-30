using Raylib_cs;
using System.Numerics;

namespace RogueDefense.Core;

public static class Input
{
    private const MouseButton LEFT  = (MouseButton)0;
    private const MouseButton RIGHT = (MouseButton)1;

    public static Vector2 MouseScreen => Raylib.GetMousePosition();
    public static bool LeftPressed  => Raylib.IsMouseButtonPressed(LEFT);
    public static bool RightPressed => Raylib.IsMouseButtonPressed(RIGHT);
    public static bool LeftDown     => Raylib.IsMouseButtonDown(LEFT);

    public static bool KeyPressed(KeyboardKey key) => Raylib.IsKeyPressed(key);

    public static void Update() { }
}
