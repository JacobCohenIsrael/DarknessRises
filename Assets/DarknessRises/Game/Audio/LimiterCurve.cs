using System;

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// The Web limiter's static curve, as the browser's <c>DynamicsCompressorNode</c> applies it with a hard knee: a peak
    /// over the threshold comes out over it by only 1/ratio as much. The browser also adds its own "makeup" gain to every
    /// sound, which would make the whole game louder; <see cref="CompensationGain"/> takes it back off, so sounds under
    /// the threshold play exactly as they would without the limiter. Plain C# (ADR 0003).
    /// </summary>
    public readonly struct LimiterCurve
    {
        // The Web Audio compressor's makeup: (1 / the curve's gain at full scale) to this power (Chromium, WebKit, Gecko).
        private const double MakeupExponent = 0.6;

        public LimiterCurve(float thresholdDecibels, float ratio)
        {
            if (!(thresholdDecibels <= 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(thresholdDecibels), thresholdDecibels, "The threshold is at or below full scale.");
            }

            if (!(ratio >= 1f))
            {
                throw new ArgumentOutOfRangeException(nameof(ratio), ratio, "A ratio below 1 would make peaks louder.");
            }

            ThresholdDecibels = thresholdDecibels;
            Ratio = ratio;
        }

        public float ThresholdDecibels { get; }

        public float Ratio { get; }

        /// <summary>The gain the browser adds to everything, as a factor.</summary>
        public float MakeupGain => (float)Math.Pow(1.0 / ToGain(Limit(0f)), MakeupExponent);

        /// <summary>The factor after the limiter that takes the browser's makeup gain back off.</summary>
        public float CompensationGain => 1f / MakeupGain;

        /// <summary>
        /// The level in dBFS a steady signal at <paramref name="inputDecibels"/> comes out at, the makeup taken back off:
        /// unchanged up to the threshold, then rising only 1/ratio as fast.
        /// </summary>
        public float Limit(float inputDecibels)
        {
            return inputDecibels <= ThresholdDecibels ? inputDecibels : ThresholdDecibels + (inputDecibels - ThresholdDecibels) / Ratio;
        }

        private static double ToGain(float decibels) => Math.Pow(10.0, decibels / 20.0);
    }
}
