using System;

namespace DarknessRises.Game.Health
{
    /// <summary>The numbers <see cref="HurtFeedback"/> runs on: the hit's flash, the low threshold and the heartbeat's timing.</summary>
    public readonly struct HurtFeedbackTiming
    {
        public HurtFeedbackTiming(
            float flashStrengthPerHitPoint,
            float flashMaxStrength,
            float flashSeconds,
            int lowHitPoints,
            float beatPeriodSeconds,
            float dubDelaySeconds,
            float dubStrength,
            float pulseDecaySeconds,
            float pulseRestStrength,
            float pulsePeakStrength)
        {
            if (flashSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(flashSeconds), flashSeconds, "The flash must last some time.");
            }

            if (beatPeriodSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(beatPeriodSeconds), beatPeriodSeconds, "A heartbeat needs a period.");
            }

            if (dubDelaySeconds < 0f || dubDelaySeconds >= beatPeriodSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(dubDelaySeconds), dubDelaySeconds, "The soft beat comes within the period, after the strong one.");
            }

            if (pulseDecaySeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(pulseDecaySeconds), pulseDecaySeconds, "A beat's swell must last some time.");
            }

            FlashStrengthPerHitPoint = flashStrengthPerHitPoint;
            FlashMaxStrength = flashMaxStrength;
            FlashSeconds = flashSeconds;
            LowHitPoints = lowHitPoints;
            BeatPeriodSeconds = beatPeriodSeconds;
            DubDelaySeconds = dubDelaySeconds;
            DubStrength = dubStrength;
            PulseDecaySeconds = pulseDecaySeconds;
            PulseRestStrength = pulseRestStrength;
            PulsePeakStrength = pulsePeakStrength;
        }

        public float FlashStrengthPerHitPoint { get; }
        public float FlashMaxStrength { get; }
        public float FlashSeconds { get; }
        public int LowHitPoints { get; }
        public float BeatPeriodSeconds { get; }
        public float DubDelaySeconds { get; }
        public float DubStrength { get; }
        public float PulseDecaySeconds { get; }
        public float PulseRestStrength { get; }
        public float PulsePeakStrength { get; }
    }
}
