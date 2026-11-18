using System;
using DarknessRises.Game.Randomness;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// How one Rumble differs from the last: a pitch and a start point in the clip, each picked at random within an
    /// authored range, so no two strikes sound the same.
    /// </summary>
    public readonly struct RumbleVariation
    {
        public RumbleVariation(float minPitch, float maxPitch, float minClipStartSeconds, float maxClipStartSeconds)
        {
            if (minPitch <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(minPitch), minPitch, "A Rumble's pitch must be positive.");
            }

            if (maxPitch < minPitch)
            {
                throw new ArgumentOutOfRangeException(nameof(maxPitch), maxPitch, "The highest pitch can't be lower than the lowest.");
            }

            if (minClipStartSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(minClipStartSeconds), minClipStartSeconds, "The clip can't start before its beginning.");
            }

            if (maxClipStartSeconds < minClipStartSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(maxClipStartSeconds), maxClipStartSeconds, "The latest start can't be earlier than the earliest.");
            }

            MinPitch = minPitch;
            MaxPitch = maxPitch;
            MinClipStartSeconds = minClipStartSeconds;
            MaxClipStartSeconds = maxClipStartSeconds;
        }

        public float MinPitch { get; }
        public float MaxPitch { get; }
        public float MinClipStartSeconds { get; }
        public float MaxClipStartSeconds { get; }

        /// <summary>Picks the next Rumble's pitch, then its start point.</summary>
        public RumbleTake Pick(IRandom random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var pitch = random.Range(MinPitch, MaxPitch);
            var clipStartSeconds = random.Range(MinClipStartSeconds, MaxClipStartSeconds);
            return new RumbleTake(pitch, clipStartSeconds);
        }
    }

    /// <summary>One Rumble as it plays: its pitch and where in the clip it starts.</summary>
    public readonly struct RumbleTake
    {
        public RumbleTake(float pitch, float clipStartSeconds)
        {
            Pitch = pitch;
            ClipStartSeconds = clipStartSeconds;
        }

        public float Pitch { get; }
        public float ClipStartSeconds { get; }
    }
}
