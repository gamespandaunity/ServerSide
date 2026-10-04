using System;

/// <summary>
/// Per-delivery seeded RNG for the deterministic-lockstep rework (see Docs/DETERMINISTIC_LOCKSTEP.md).
/// Both clients seed from the same relayed deliverySeed at ball release, so every gameplay roll drawn
/// through here lands identically on both machines. Built on System.Random (not UnityEngine.Random) so
/// cosmetic randomness elsewhere can never perturb the gameplay stream. The draw ORDER must match on
/// both clients — only the lockstep-resolved paths (shot tables, power, friction, fence rebound) may
/// draw from this.
/// </summary>
public static class DeterministicRng
{
    private static Random _rng = new Random(0);
    public static int CurrentSeed { get; private set; }

    /// <summary>Draws consumed since the last Seed — logged on both clients to pinpoint sequence misalignment.</summary>
    public static int Count { get; private set; }

    public static void Seed(int seed)
    {
        CurrentSeed = seed;
        Count = 0;
        _rng = new Random(seed);
    }

    /// <summary>Unity-style float range.</summary>
    public static float Range(float min, float max)
    {
        Count++;
        return min + (float)_rng.NextDouble() * (max - min);
    }

    /// <summary>Unity-style int range (max EXCLUSIVE, matching UnityEngine.Random.Range).</summary>
    public static int Range(int min, int max)
    {
        Count++;
        return _rng.Next(min, max);
    }
}
