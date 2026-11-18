using System;
using DarknessRises.Game.Randomness;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// How much each strike's angle varies, so no two flashes throw quite the same shadows: an elevation from an
    /// authored range, and a yaw jittered up to an authored number of degrees either side of the Room's Lightning
    /// direction.
    /// </summary>
    public readonly struct LightningAngles
    {
        public LightningAngles(float minElevationDegrees, float maxElevationDegrees, float yawJitterDegrees)
        {
            if (minElevationDegrees <= 0f || maxElevationDegrees >= 90f)
            {
                throw new ArgumentOutOfRangeException(nameof(minElevationDegrees), "Lightning comes in from above the horizon and below the zenith.");
            }

            if (maxElevationDegrees < minElevationDegrees)
            {
                throw new ArgumentOutOfRangeException(nameof(maxElevationDegrees), maxElevationDegrees, "The highest elevation can't be lower than the lowest.");
            }

            if (yawJitterDegrees < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(yawJitterDegrees), yawJitterDegrees, "The yaw jitter can't be negative.");
            }

            MinElevationDegrees = minElevationDegrees;
            MaxElevationDegrees = maxElevationDegrees;
            YawJitterDegrees = yawJitterDegrees;
        }

        public float MinElevationDegrees { get; }
        public float MaxElevationDegrees { get; }
        public float YawJitterDegrees { get; }

        /// <summary>The middle of the elevation range: where Lightning comes in from before the first strike.</summary>
        public float MiddleElevationDegrees => (MinElevationDegrees + MaxElevationDegrees) * 0.5f;

        /// <summary>Picks the next strike's angle around the Room's Lightning yaw: elevation first, then yaw.</summary>
        public LightningAngle Pick(IRandom random, float roomYawDegrees)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var elevation = random.Range(MinElevationDegrees, MaxElevationDegrees);
            var yaw = roomYawDegrees + random.Range(-YawJitterDegrees, YawJitterDegrees);
            return new LightningAngle(elevation, yaw);
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

        /// <summary>Degrees of yaw, as for the Room's Lightning direction.</summary>
        public float YawDegrees { get; }
    }
}
