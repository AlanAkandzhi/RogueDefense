namespace RogueDefense.Gameplay;

public enum StatusKind { Slow, Burn }

public sealed class StatusEffect
{
    public StatusKind Kind;
    public float Duration;
    public float Magnitude;

    public StatusEffect(StatusKind kind, float duration, float magnitude)
    {
        Kind = kind; Duration = duration; Magnitude = magnitude;
    }
}
