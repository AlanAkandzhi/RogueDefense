using RogueDefense.Core;

namespace RogueDefense.Rendering;
using RogueDefense.Gameplay;

public sealed class Ui
{
    private readonly GameState _s;
    public Ui(GameState s) { _s = s; }

    public void Update(float dt)
    {
        //menus, meta-progression, pause, etc.
        //Could add a meta-shop between waves here.
    }
}
