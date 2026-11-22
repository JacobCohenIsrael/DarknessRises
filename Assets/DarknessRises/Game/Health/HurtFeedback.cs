using System;

namespace DarknessRises.Game.Health
{
    /// <summary>
    /// The red at the screen's edges that says the character is hurt, and the heartbeat with it. A landed hit flashes
    /// the edge, stronger for a bigger hit, fading out over a short moment. On the last few hit points a slow heartbeat
    /// loops, and on the very last a fast one, crossfading between them; the edge pulses in time with whichever is
    /// heard, read from the loops' own playback time so it never drifts. Plain C# (ADR 0003): time comes in as an
    /// explicit delta.
    /// </summary>
    /// <remarks>
    /// The heartbeat lasts while the character is alive, short of full health, and at or under the low threshold.
    /// Death, a Continue (back to full) and <see cref="Stop"/> (the Level's end) silence it at once, with no fade.
    /// </remarks>
    public sealed class HurtFeedback
    {
        private readonly HurtFeedbackTiming timing;
        private float flashStrength;
        private float flashSecondsLeft;

        public HurtFeedback(HurtFeedbackTiming timing)
        {
            this.timing = timing;
        }

        /// <summary>Which heartbeat the hit points call for now.</summary>
        public HeartbeatTier Tier { get; private set; }

        /// <summary>Whether a heartbeat plays at all: the character is on its last few hit points.</summary>
        public bool IsLow => Tier != HeartbeatTier.None;

        /// <summary>How loud the slow loop plays, from 0 to 1, before its authored volume.</summary>
        public float SlowWeight { get; private set; }

        /// <summary>How loud the fast loop plays, from 0 to 1, before its authored volume.</summary>
        public float FastWeight { get; private set; }

        /// <summary>The hit's flash, fading out over its seconds.</summary>
        public float FlashStrength => flashSecondsLeft > 0f ? flashStrength * (flashSecondsLeft / timing.FlashSeconds) : 0f;

        /// <summary>How strongly the edge shows: the stronger of the hit's flash and the pulse, the loops this far in.</summary>
        public float Strength(float slowClipSeconds, float fastClipSeconds) =>
            Math.Max(FlashStrength, PulseStrength(slowClipSeconds, fastClipSeconds));

        /// <summary>
        /// The low-health pulse with the loops <paramref name="slowClipSeconds"/> and <paramref name="fastClipSeconds"/>
        /// in: it rests at its quiet strength and swells with each beat of the loops heard, the soft beat less.
        /// </summary>
        public float PulseStrength(float slowClipSeconds, float fastClipSeconds)
        {
            if (!IsLow)
            {
                return 0f;
            }

            var beat = SlowWeight * Swell(timing.Slow, slowClipSeconds) + FastWeight * Swell(timing.Fast, fastClipSeconds);
            beat = Math.Min(1f, beat);
            return timing.PulseRestStrength + (timing.PulsePeakStrength - timing.PulseRestStrength) * beat;
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
        /// The character now has <paramref name="hitPoints"/> of <paramref name="maxHitPoints"/>. A heartbeat starting
        /// starts at full; one tier to the other crossfades; leaving low silences both at once.
        /// </summary>
        public void SetHitPoints(int hitPoints, int maxHitPoints)
        {
            var tier = TierAt(hitPoints, maxHitPoints, timing.LowHitPoints, timing.CriticalHitPoints);
            if (tier == Tier)
            {
                return;
            }

            var wasLow = IsLow;
            Tier = tier;
            if (tier == HeartbeatTier.None)
            {
                SlowWeight = FastWeight = 0f;
            }
            else if (!wasLow || timing.CrossfadeSeconds <= 0f)
            {
                SlowWeight = tier == HeartbeatTier.Slow ? 1f : 0f;
                FastWeight = tier == HeartbeatTier.Fast ? 1f : 0f;
            }
        }

        /// <summary>Clears the edge and silences the heartbeat at once, for the Level's end.</summary>
        public void Stop()
        {
            Tier = HeartbeatTier.None;
            SlowWeight = FastWeight = 0f;
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

            var step = timing.CrossfadeSeconds > 0f ? deltaSeconds / timing.CrossfadeSeconds : 1f;
            SlowWeight = Toward(SlowWeight, Tier == HeartbeatTier.Slow ? 1f : 0f, step);
            FastWeight = Toward(FastWeight, Tier == HeartbeatTier.Fast ? 1f : 0f, step);
        }

        /// <summary>
        /// The heartbeat <paramref name="hitPoints"/> of <paramref name="maxHitPoints"/> calls for: none at full health
        /// or dead, fast at or under <paramref name="criticalHitPoints"/>, slow at or under <paramref name="lowHitPoints"/>.
        /// </summary>
        public static HeartbeatTier TierAt(int hitPoints, int maxHitPoints, int lowHitPoints, int criticalHitPoints)
        {
            if (hitPoints <= 0 || hitPoints >= maxHitPoints)
            {
                return HeartbeatTier.None;
            }

            if (hitPoints <= criticalHitPoints)
            {
                return HeartbeatTier.Fast;
            }

            return hitPoints <= lowHitPoints ? HeartbeatTier.Slow : HeartbeatTier.None;
        }

        // A beat's swell in one loop: full at the strong beat and easing out, the soft beat after it swelling less.
        private float Swell(HeartbeatRhythm rhythm, float clipSeconds)
        {
            var since = rhythm.SecondsSinceBeat(clipSeconds);
            var strong = Decay(since);
            var soft = Decay(since - rhythm.SoftBeatDelaySeconds) * timing.SoftBeatStrength;
            return Math.Max(strong, soft);
        }

        private float Decay(float secondsSinceBeat)
        {
            if (secondsSinceBeat < 0f || secondsSinceBeat >= timing.PulseDecaySeconds)
            {
                return 0f;
            }

            var left = 1f - secondsSinceBeat / timing.PulseDecaySeconds;
            return left * left;
        }

        private static float Toward(float value, float target, float step) =>
            value < target ? Math.Min(target, value + step) : Math.Max(target, value - step);
    }
}
