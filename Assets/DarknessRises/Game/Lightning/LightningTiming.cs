using System;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// When ambient Lightning flashes: a random interval between flashes, and how long before each flash the Rumble starts.
    /// </summary>
    public readonly struct LightningTiming
    {
        public LightningTiming(float minIntervalSeconds, float maxIntervalSeconds, float rumbleLeadSeconds)
        {
            if (minIntervalSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(minIntervalSeconds), minIntervalSeconds, "The shortest interval must be positive.");
            }

            if (maxIntervalSeconds < minIntervalSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(maxIntervalSeconds), maxIntervalSeconds, "The longest interval can't be shorter than the shortest.");
            }

            if (rumbleLeadSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(rumbleLeadSeconds), rumbleLeadSeconds, "The Rumble lead can't be negative.");
            }

            MinIntervalSeconds = minIntervalSeconds;
            MaxIntervalSeconds = maxIntervalSeconds;
            RumbleLeadSeconds = rumbleLeadSeconds;
        }

        /// <summary>Shortest time from one ambient flash to the next.</summary>
        public float MinIntervalSeconds { get; }

        /// <summary>Longest time from one ambient flash to the next.</summary>
        public float MaxIntervalSeconds { get; }

        /// <summary>How long before each flash the Rumble starts.</summary>
        public float RumbleLeadSeconds { get; }
    }
}
