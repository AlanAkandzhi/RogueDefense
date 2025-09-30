namespace RogueDefense.Core;

public static class Rand
{
    private static readonly Random _rng = new();

    public static int Range(int min, int maxInclusive) => _rng.Next(min, maxInclusive + 1);
    public static float Range(float min, float max) => (float)(_rng.NextDouble() * (max - min) + min);
    public static bool Chance(float p) => _rng.NextDouble() < p;
    public static T Choice<T>(IReadOnlyList<T> list) => list[_rng.Next(list.Count)];
    public static void Shuffle<T>(IList<T> list) {
        for (int i = list.Count - 1; i > 0; i--) {
            int j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
