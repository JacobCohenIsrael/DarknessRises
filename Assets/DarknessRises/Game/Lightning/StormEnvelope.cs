using System;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// How the storm bed sounds over time: its resting volume, how fast it fades out and back in as Lightning stops
    /// and starts reaching the character, and the shape of the Rumble, the swell after each flash.
    /// </summary>
    public readonly struct StormEnvelope
    {
        public StormEnvelope(
            float baseVolume,
            float fadeInSeconds,
            float fadeOutSeconds,
            float swellPeakVolume,
            float swellAttackSeconds,
            float swellHoldSeconds,
            float swellReleaseSeconds,
            float minSwellPitch,
            float maxSwellPitch,
            float hushFadeOutSeconds,
            float hushFadeInSeconds)
        {
            if (baseVolume < 0f || baseVolume > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(baseVolume), baseVolume, "A volume is between 0 and 1.");
            }

            if (swellPeakVolume < baseVolume || swellPeakVolume > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(swellPeakVolume), swellPeakVolume, "The swell's peak is between the base volume and 1.");
            }

            if (fadeInSeconds < 0f || fadeOutSeconds < 0f || swellAttackSeconds < 0f || swellHoldSeconds < 0f || swellReleaseSeconds < 0f
                || hushFadeOutSeconds < 0f || hushFadeInSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fadeInSeconds), "The storm's fades and swell can't take negative time.");
            }

            if (minSwellPitch <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(minSwellPitch), minSwellPitch, "A pitch must be positive.");
            }

            if (maxSwellPitch < minSwellPitch)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSwellPitch), maxSwellPitch, "The highest pitch can't be lower than the lowest.");
            }

            BaseVolume = baseVolume;
            FadeInSeconds = fadeInSeconds;
            FadeOutSeconds = fadeOutSeconds;
            SwellPeakVolume = swellPeakVolume;
            SwellAttackSeconds = swellAttackSeconds;
            SwellHoldSeconds = swellHoldSeconds;
            SwellReleaseSeconds = swellReleaseSeconds;
            MinSwellPitch = minSwellPitch;
            MaxSwellPitch = maxSwellPitch;
            HushFadeOutSeconds = hushFadeOutSeconds;
            HushFadeInSeconds = hushFadeInSeconds;
        }

        /// <summary>The bed's volume between Rumbles.</summary>
        public float BaseVolume { get; }

        /// <summary>Seconds to fade in from silence on entering a Room with windows.</summary>
        public float FadeInSeconds { get; }

        /// <summary>Seconds to fade out to silence in a Room without windows, or with Lightning switched off.</summary>
        public float FadeOutSeconds { get; }

        /// <summary>The volume at the top of a Rumble.</summary>
        public float SwellPeakVolume { get; }

        /// <summary>Seconds a Rumble takes to rise to its peak.</summary>
        public float SwellAttackSeconds { get; }

        /// <summary>Seconds a Rumble stays at its peak.</summary>
        public float SwellHoldSeconds { get; }

        /// <summary>Seconds a Rumble takes to settle back to the base volume.</summary>
        public float SwellReleaseSeconds { get; }

        /// <summary>The lowest pitch a Rumble nudges the bed to.</summary>
        public float MinSwellPitch { get; }

        /// <summary>The highest pitch a Rumble nudges the bed to.</summary>
        public float MaxSwellPitch { get; }

        /// <summary>Seconds to fade out to silence once hushed: at the end of a Level, and on the way to the Shop.</summary>
        public float HushFadeOutSeconds { get; }

        /// <summary>Seconds to fade back in after a hush, once the next Level plays: gentler than a Room's fade in.</summary>
        public float HushFadeInSeconds { get; }
    }
}
