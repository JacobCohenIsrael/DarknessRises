using System;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// When ambient Lightning flashes, and when its Rumble follows: a random interval between flashes, and a random delay
    /// from each flash to its Rumble, like a strike at a varying distance.
    /// </summary>
    public readonly struct LightningTiming
    {
        public LightningTiming(
            float minIntervalSeconds,
            float maxIntervalSeconds,
            float minRumbleDelaySeconds,
            float maxRumbleDelaySeconds)
        {
            if (minIntervalSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(minIntervalSeconds), minIntervalSeconds, "The shortest interval must be positive.");
            }

            if (maxIntervalSeconds < minIntervalSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(maxIntervalSeconds), maxIntervalSeconds, "The longest interval can't be shorter than the shortest.");
            }

            if (minRumbleDelaySeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(minRumbleDelaySeconds), minRumbleDelaySeconds, "The Rumble can't come before the flash.");
            }

            if (maxRumbleDelaySeconds < minRumbleDelaySeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRumbleDelaySeconds), maxRumbleDelaySeconds, "The longest Rumble delay can't be shorter than the shortest.");
            }

            MinIntervalSeconds = minIntervalSeconds;
            MaxIntervalSeconds = maxIntervalSeconds;
            MinRumbleDelaySeconds = minRumbleDelaySeconds;
            MaxRumbleDelaySeconds = maxRumbleDelaySeconds;
        }

        /// <summary>Shortest time from one ambient flash to the next.</summary>
        public float MinIntervalSeconds { get; }

        /// <summary>Longest time from one ambient flash to the next.</summary>
        public float MaxIntervalSeconds { get; }

        /// <summary>Shortest time from a flash to its Rumble.</summary>
        public float MinRumbleDelaySeconds { get; }

        /// <summary>Longest time from a flash to its Rumble.</summary>
        public float MaxRumbleDelaySeconds { get; }
    }
}
