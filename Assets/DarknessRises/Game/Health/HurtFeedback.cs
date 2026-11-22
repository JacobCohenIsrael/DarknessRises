using System;

namespace DarknessRises.Game.Health
{
    /// <summary>
    /// The red at the screen's edges that says the character is hurt. A landed hit flashes it, stronger for a bigger
    /// hit, fading out over a short moment. On the last few hit points it pulses slowly with a heartbeat, a strong
    /// beat and a softer one after it, and asks for the heartbeat sound on each. Plain C# (ADR 0003): time comes in
    /// as an explicit delta.
    /// </summary>
    /// <remarks>
    /// Being low lasts while the character is alive, short of full health, and at or under the low threshold. Death,
    /// a Continue (back to full) and <see cref="Stop"/> (the Level's end) end it at once.
    /// </remarks>
    public sealed class HurtFeedback
    {
        private readonly HurtFeedbackTiming timing;
        private float flashStrength;
        private float flashSecondsLeft;
        private float beatSeconds;
        private bool dubPlayed;

        public HurtFeedback(HurtFeedbackTiming timing)
        {
            this.timing = timing;
        }

        /// <summary>Raised on each heartbeat while low: <c>false</c> for the strong beat, <c>true</c> for the soft one after it.</summary>
        public event Action<bool> Beat;

        /// <summary>Whether the character is on its last few hit points, so the edge pulses with the heartbeat.</summary>
        public bool IsLow { get; private set; }

        /// <summary>How strongly the edge shows now, from 0 (clear) to 1: the stronger of the hit's flash and the pulse.</summary>
        public float Strength => Math.Max(FlashStrength, PulseStrength);

        /// <summary>The hit's flash, fading out over its seconds.</summary>
        public float FlashStrength => flashSecondsLeft > 0f ? flashStrength * (flashSecondsLeft / timing.FlashSeconds) : 0f;

        /// <summary>The low-health pulse: rests at its quiet strength, jumping with each beat and easing back.</summary>
        public float PulseStrength
        {
            get
            {
                if (!IsLow)
                {
                    return 0f;
                }

                var lub = Decay(beatSeconds);
                var dub = Decay(beatSeconds - timing.DubDelaySeconds) * timing.DubStrength;
                var beat = Math.Max(lub, dub);
                return timing.PulseRestStrength + (timing.PulsePeakStrength - timing.PulseRestStrength) * beat;
            }
        }

        /// <summary>A hit of <paramref name="damage"/> hit points landed: flashes the edge, stronger for a bigger hit.</summary>
        public void Hit(int damage)
        {
            if (damage <= 0)
            {
                return;
            }

            var strength = Math.Min(timing.FlashMaxStrength, timing.FlashStrengthPerHitPoint * damage);
            flashStrength = Math.Max(strength, FlashStrength);
            flashSecondsLeft = timing.FlashSeconds;
        }

        /// <summary>
        /// The character now has <paramref name="hitPoints"/> of <paramref name="maxHitPoints"/>. Going low starts the
        /// heartbeat with a beat on the next tick; leaving it (dead, or back to full) stops it at once.
        /// </summary>
        public void SetHitPoints(int hitPoints, int maxHitPoints)
        {
            var low = IsLowAt(hitPoints, maxHitPoints, timing.LowHitPoints);
            if (low == IsLow)
            {
                return;
            }

            IsLow = low;
            beatSeconds = -1f;
            dubPlayed = false;
        }

        /// <summary>Clears the edge and stops the heartbeat at once, for the Level's end.</summary>
        public void Stop()
        {
            IsLow = false;
            flashSecondsLeft = 0f;
            flashStrength = 0f;
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            flashSecondsLeft = Math.Max(0f, flashSecondsLeft - deltaSeconds);
            if (!IsLow)
            {
                return;
            }

            // A fresh low starts on a beat; after that, a beat each period, the soft beat a moment after the strong one.
            if (beatSeconds < 0f)
            {
                beatSeconds = 0f;
                dubPlayed = false;
                Beat?.Invoke(false);
            }
            else
            {
                beatSeconds += deltaSeconds;
                if (beatSeconds >= timing.BeatPeriodSeconds)
                {
                    beatSeconds %= timing.BeatPeriodSeconds;
                    dubPlayed = false;
                    Beat?.Invoke(false);
                }
            }

            if (!dubPlayed && beatSeconds >= timing.DubDelaySeconds)
            {
                dubPlayed = true;
                Beat?.Invoke(true);
            }
        }

        /// <summary>Whether <paramref name="hitPoints"/> of <paramref name="maxHitPoints"/> is low: alive, short of full, and at or under <paramref name="lowHitPoints"/>.</summary>
        public static bool IsLowAt(int hitPoints, int maxHitPoints, int lowHitPoints)
        {
            return hitPoints > 0 && hitPoints < maxHitPoints && hitPoints <= lowHitPoints;
        }

        // A beat's swell: full at the beat, easing to nothing over the pulse's decay.
        private float Decay(float secondsSinceBeat)
        {
            if (secondsSinceBeat < 0f || secondsSinceBeat >= timing.PulseDecaySeconds)
            {
                return 0f;
            }

            var left = 1f - secondsSinceBeat / timing.PulseDecaySeconds;
            return left * left;
        }
    }
}
