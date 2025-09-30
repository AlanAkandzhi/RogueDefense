using System.Numerics;

namespace RogueDefense.Gameplay;

public sealed class Particle
{
    public Vector2 Pos;
    public Vector2 Vel;
    public float Time;
    public float Lifetime;

    public Particle()
    {
        Lifetime = Time;
    }

    public void Update(float dt)
    {
        if (Lifetime <= 0) Lifetime = MathF.Max(Time, 0.001f);
        Pos += Vel * dt;
        Time -= dt;
    }
}
