using System;

namespace DarknessRises.Game.Health
{
    /// <summary>
    /// The numbers <see cref="HurtFeedback"/> runs on: the hit's flash, the low and critical thresholds, the crossfade
    /// between the heartbeats, and how the edge swells with each beat.
    /// </summary>
    public readonly struct HurtFeedbackTiming
    {
        public HurtFeedbackTiming(
            float flashStrengthPerHitPoint,
            float flashMaxStrength,
            float flashSeconds,
            int lowHitPoints,
            int criticalHitPoints,
            float crossfadeSeconds,
            float pulseDecaySeconds,
            float pulseRestStrength,
            float pulsePeakStrength,
            float softBeatStrength,
            HeartbeatRhythm slow,
            HeartbeatRhythm fast)
        {
            if (flashSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(flashSeconds), flashSeconds, "The flash must last some time.");
            }

            if (crossfadeSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(crossfadeSeconds), crossfadeSeconds, "A crossfade can't take negative time.");
            }

            if (pulseDecaySeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(pulseDecaySeconds), pulseDecaySeconds, "A beat's swell must last some time.");
            }

            FlashStrengthPerHitPoint = flashStrengthPerHitPoint;
            FlashMaxStrength = flashMaxStrength;
            FlashSeconds = flashSeconds;
            LowHitPoints = lowHitPoints;
            CriticalHitPoints = criticalHitPoints;
            CrossfadeSeconds = crossfadeSeconds;
            PulseDecaySeconds = pulseDecaySeconds;
            PulseRestStrength = pulseRestStrength;
            PulsePeakStrength = pulsePeakStrength;
            SoftBeatStrength = softBeatStrength;
            Slow = slow;
            Fast = fast;
        }

        public float FlashStrengthPerHitPoint { get; }
        public float FlashMaxStrength { get; }
        public float FlashSeconds { get; }
        public int LowHitPoints { get; }
        public int CriticalHitPoints { get; }
        public float CrossfadeSeconds { get; }
        public float PulseDecaySeconds { get; }
        public float PulseRestStrength { get; }
        public float PulsePeakStrength { get; }
        public float SoftBeatStrength { get; }
        public HeartbeatRhythm Slow { get; }
        public HeartbeatRhythm Fast { get; }
    }
}
