using System;
using DarknessRises.Game.Randomness;
using UnityEngine;

namespace DarknessRises.Game.Phantoms
{
    /// <summary>
    /// When a pair of Phantom eyes that held its time blinks out, it sometimes opens again a moment later at another dark
    /// spot nearby: a "was that the same thing?" moment. Only a pair that ran out its time hops; one sent away by light,
    /// aim, an Enemy or a fight never does, and a pair hops a few times at most. The new spot must pass every rule a
    /// first spot does. Plain C# (ADR 0003).
    /// </summary>
    public readonly struct PhantomRelocation
    {
        /// <param name="chance">Chance, from 0 to 1, that a pair whose time ran out opens again nearby.</param>
        /// <param name="maxHops">Most times one pair may hop.</param>
        /// <param name="minGapSeconds">Fewest seconds of dark between blinking out and opening again.</param>
        /// <param name="maxGapSeconds">Most seconds of dark between blinking out and opening again.</param>
        /// <param name="minMetres">Fewest metres from the old spot to the new one.</param>
        /// <param name="maxMetres">Most metres from the old spot to the new one.</param>
        public PhantomRelocation(float chance, int maxHops, float minGapSeconds, float maxGapSeconds, float minMetres, float maxMetres)
        {
            if (chance < 0f || chance > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(chance), chance, "A chance runs from 0 to 1.");
            }

            if (maxHops < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHops), maxHops, "This can't be negative.");
            }

            if (minGapSeconds < 0f || maxGapSeconds < minGapSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(maxGapSeconds), maxGapSeconds, "A range runs from a non-negative least to a most at least as big.");
            }

            if (minMetres < 0f || maxMetres < minMetres)
            {
                throw new ArgumentOutOfRangeException(nameof(maxMetres), maxMetres, "A range runs from a non-negative least to a most at least as big.");
            }

            Chance = chance;
            MaxHops = maxHops;
            MinGapSeconds = minGapSeconds;
            MaxGapSeconds = maxGapSeconds;
            MinMetres = minMetres;
            MaxMetres = maxMetres;
        }

        public float Chance { get; }
        public int MaxHops { get; }
        public float MinGapSeconds { get; }
        public float MaxGapSeconds { get; }
        public float MinMetres { get; }
        public float MaxMetres { get; }

        /// <summary>Whether a pair that went away for <paramref name="vanish"/>, having hopped <paramref name="hopsDone"/> times, opens again nearby.</summary>
        public bool Hops(PhantomVanish vanish, int hopsDone, IRandom random)
        {
            return vanish == PhantomVanish.TimeUp && hopsDone < MaxHops && Chance > 0f && random.Range(0f, 1f) < Chance;
        }

        /// <summary>Seconds of dark before the hop opens.</summary>
        public float Gap(IRandom random)
        {
            return random.Range(MinGapSeconds, MaxGapSeconds);
        }

        /// <summary>A spot to hop to from <paramref name="from"/>, any way round, at its height.</summary>
        public Vector3 PickNear(Vector3 from, IRandom random)
        {
            var direction = Quaternion.AngleAxis(random.Range(0f, 360f), Vector3.up) * Vector3.forward;
            return from + direction * random.Range(MinMetres, MaxMetres);
        }
    }
}
