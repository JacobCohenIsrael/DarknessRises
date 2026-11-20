using System;
using UnityEngine;

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// Turns a volume slider's percent into how loud its sounds play. Loudness rises evenly in decibels from the
    /// quietest step (1 %) to full (100 %, 0 dB), so every step of the slider sounds like the same change; 0 % is silent.
    /// </summary>
    public readonly struct VolumeCurve
    {
        /// <summary>A slider's top: full volume, the sounds as they were authored.</summary>
        public const int MaxPercent = 100;

        /// <summary>A slider's bottom: silent.</summary>
        public const int MinPercent = 0;

        // The lowest step that is still heard.
        private const int QuietestPercent = 1;

        private const float DecibelsPerAmplitudeDecade = 20f;
        private const float AmplitudeBase = 10f;

        /// <param name="quietestDecibels">How loud the quietest step (1 %) is, in decibels below full. Below zero.</param>
        public VolumeCurve(float quietestDecibels)
        {
            if (!(quietestDecibels < 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(quietestDecibels), quietestDecibels, "The quietest step must be below full volume (0 dB).");
            }

            QuietestDecibels = quietestDecibels;
        }

        /// <summary>How loud the quietest step (1 %) is, in decibels below full.</summary>
        public float QuietestDecibels { get; }

        /// <summary><paramref name="percent"/> kept between <see cref="MinPercent"/> and <see cref="MaxPercent"/>.</summary>
        public static int Clamp(int percent) => Math.Clamp(percent, MinPercent, MaxPercent);

        /// <summary>The level in decibels (0 at full) at <paramref name="percent"/>; negative infinity when silent.</summary>
        public float Decibels(int percent)
        {
            percent = Clamp(percent);
            if (percent < QuietestPercent)
            {
                return float.NegativeInfinity;
            }

            var belowFull = (float)(MaxPercent - percent) / (MaxPercent - QuietestPercent);
            return QuietestDecibels * belowFull;
        }

        /// <summary>
        /// The factor an <see cref="AudioSource"/>'s own volume is multiplied by at <paramref name="percent"/>:
        /// 1 at full, 0 when silent.
        /// </summary>
        public float Gain(int percent)
        {
            var decibels = Decibels(percent);
            return float.IsNegativeInfinity(decibels) ? 0f : Mathf.Pow(AmplitudeBase, decibels / DecibelsPerAmplitudeDecade);
        }
    }
}
