using System;
using DarknessRises.Game.Randomness;
using UnityEngine;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// How much each strike's angle varies, so no two flashes throw quite the same shadows: an elevation from an
    /// authored range, and a yaw jittered up to an authored number of degrees either side of the Room's Lightning
    /// direction. Now and then a strike comes in low, from its own lower range, and throws long shadows across the floor
    /// and up the walls.
    /// </summary>
    public readonly struct LightningAngles
    {
        public LightningAngles(float minElevationDegrees, float maxElevationDegrees, float yawJitterDegrees)
            : this(minElevationDegrees, maxElevationDegrees, yawJitterDegrees, 0f, minElevationDegrees, minElevationDegrees)
        {
        }

        public LightningAngles(
            float minElevationDegrees,
            float maxElevationDegrees,
            float yawJitterDegrees,
            float lowChance,
            float minLowElevationDegrees,
            float maxLowElevationDegrees)
        {
            CheckRange(minElevationDegrees, maxElevationDegrees);
            CheckRange(minLowElevationDegrees, maxLowElevationDegrees);

            if (yawJitterDegrees < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(yawJitterDegrees), yawJitterDegrees, "The yaw jitter can't be negative.");
            }

            if (lowChance < 0f || lowChance > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(lowChance), lowChance, "A chance is from 0 to 1.");
            }

            MinElevationDegrees = minElevationDegrees;
            MaxElevationDegrees = maxElevationDegrees;
            YawJitterDegrees = yawJitterDegrees;
            LowChance = lowChance;
            MinLowElevationDegrees = minLowElevationDegrees;
            MaxLowElevationDegrees = maxLowElevationDegrees;
        }

        public float MinElevationDegrees { get; }
        public float MaxElevationDegrees { get; }
        public float YawJitterDegrees { get; }

        /// <summary>The chance, from 0 to 1, that a strike comes in from the low range instead.</summary>
        public float LowChance { get; }

        public float MinLowElevationDegrees { get; }
        public float MaxLowElevationDegrees { get; }

        /// <summary>The middle of the elevation range: where Lightning comes in from before the first strike.</summary>
        public float MiddleElevationDegrees => (MinElevationDegrees + MaxElevationDegrees) * 0.5f;

        /// <summary>
        /// Picks the next strike's angle around the Room's Lightning yaw: whether it comes in low (only drawn when there's
        /// a chance), then its elevation, then its yaw.
        /// </summary>
        public LightningAngle Pick(IRandom random, float roomYawDegrees)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var low = Chance.Roll(random, LowChance);
            var elevation = low
                ? random.Range(MinLowElevationDegrees, MaxLowElevationDegrees)
                : random.Range(MinElevationDegrees, MaxElevationDegrees);
            var yaw = roomYawDegrees + random.Range(-YawJitterDegrees, YawJitterDegrees);
            return new LightningAngle(elevation, yaw);
        }

        private static void CheckRange(float min, float max)
        {
            if (min <= 0f || max >= 90f)
            {
                throw new ArgumentOutOfRangeException(nameof(min), "Lightning comes in from above the horizon and below the zenith.");
            }

            if (max < min)
            {
                throw new ArgumentOutOfRangeException(nameof(max), max, "The highest elevation can't be lower than the lowest.");
            }
        }
    }

    /// <summary>The direction one strike comes in from.</summary>
    public readonly struct LightningAngle
    {
        public LightningAngle(float elevationDegrees, float yawDegrees)
        {
            ElevationDegrees = elevationDegrees;
            YawDegrees = yawDegrees;
        }

        /// <summary>Degrees above the horizon.</summary>
        public float ElevationDegrees { get; }

        /// <summary>Degrees of yaw, as for the Room's Lightning direction: the way the light travels across the floor.</summary>
        public float YawDegrees { get; }

        /// <summary>
        /// How much brighter the key light shines at this elevation than at <paramref name="referenceElevationDegrees"/>
        /// so the floor is lit as brightly, up to <paramref name="maxBoost"/>: light that comes in low spreads over more
        /// floor, so without it a low strike would barely reveal the Room.
        /// </summary>
        public float FloorBrightnessBoost(float referenceElevationDegrees, float maxBoost)
        {
            var here = Mathf.Sin(ElevationDegrees * Mathf.Deg2Rad);
            var reference = Mathf.Sin(referenceElevationDegrees * Mathf.Deg2Rad);
            if (here <= 0f)
            {
                return maxBoost;
            }

            return Mathf.Clamp(reference / here, 1f, Mathf.Max(1f, maxBoost));
        }
    }

    /// <summary>A roll against a chance from 0 to 1 that draws nothing when the outcome is certain.</summary>
    internal static class Chance
    {
        public static bool Roll(IRandom random, float chance)
        {
            if (chance <= 0f)
            {
                return false;
            }

            return chance >= 1f || random.Range(0f, 1f) < chance;
        }
    }
}
