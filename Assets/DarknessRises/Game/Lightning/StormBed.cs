using System;
using DarknessRises.Game.Randomness;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// The storm bed: the thunder loop that plays all the time Lightning reaches the character, and the Rumble, the swell
    /// it gives after each flash. Works out the bed's volume and pitch over time; every change is a ramp from where it
    /// is now, so the storm never jumps, starts or stops dead.
    /// Plain C# (ADR 0003); <see cref="LightningView"/> applies <see cref="Volume"/> and <see cref="Pitch"/> to its AudioSource.
    /// </summary>
    public sealed class StormBed
    {
        private const float NormalPitch = 1f;

        private enum SwellPhase
        {
            Resting,
            Attack,
            Hold,
            Release,
        }

        private readonly StormEnvelope envelope;

        private bool audible;
        private bool hushed;

        // Fading back in after a hush, at the gentler hush fade-in, until fully in or told to fade out again.
        private bool returningFromHush;
        private float presence;
        private SwellPhase phase = SwellPhase.Resting;
        private float phaseElapsedSeconds;
        private float swellFrom;
        private float pitchFrom;
        private float swellTargetPitch = NormalPitch;
        private float swell;

        public StormBed(StormEnvelope envelope)
        {
            this.envelope = envelope;
        }

        /// <summary>From 0 (faded out) to 1 (fully in): how far the bed has faded in.</summary>
        public float Presence => presence;

        /// <summary>From 0 (the base volume) to 1 (the swell's peak): how far into a Rumble the bed is.</summary>
        public float Swell => swell;

        /// <summary>Whether a Rumble is rising, holding or settling.</summary>
        public bool IsSwelling => phase != SwellPhase.Resting;

        /// <summary>Whether the bed is fading in or playing, rather than fading out or silent.</summary>
        public bool IsAudible => audible && !hushed;

        /// <summary>Whether the bed is hushed, whatever Lightning does: see <see cref="Hush"/>.</summary>
        public bool IsHushed => hushed;

        /// <summary>The bed's volume right now.</summary>
        public float Volume => presence * (envelope.BaseVolume + (envelope.SwellPeakVolume - envelope.BaseVolume) * swell);

        /// <summary>The bed's own pitch right now: 1, nudged during a Rumble. Adrenaline is applied on top by the view.</summary>
        public float Pitch { get; private set; } = NormalPitch;

        /// <summary>
        /// Whether Lightning reaches the character (a Room with windows, or a Boss fight, with Lightning switched on).
        /// The bed fades in or out from its current volume over the authored time.
        /// </summary>
        public void SetAudible(bool isAudible)
        {
            audible = isAudible;
        }

        /// <summary>
        /// Hushes the bed whatever <see cref="SetAudible"/> says, or lets it follow that again. For whoever owns the bed
        /// beyond one Level (the boot): on the way to the Shop the Level still says the storm reaches the character until
        /// its scene is gone, and the bed has to start fading out the moment the player leaves; the same at the end of a
        /// Level. It fades out and back in from where it is, over the authored hush times.
        /// </summary>
        public void Hush(bool hush)
        {
            if (hushed && !hush)
            {
                returningFromHush = true;
            }

            hushed = hush;
        }

        /// <summary>
        /// A Rumble: the bed rises from its current volume to the peak, holds, then settles back to the base volume, with
        /// a pitch nudge picked from the authored range and eased back. A Rumble during another one carries on from
        /// wherever that one was.
        /// </summary>
        public void Rumble(IRandom random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            swellFrom = swell;
            pitchFrom = Pitch;
            swellTargetPitch = random.Range(envelope.MinSwellPitch, envelope.MaxSwellPitch);
            phase = SwellPhase.Attack;
            phaseElapsedSeconds = 0f;
        }

        /// <summary>Advances <paramref name="deltaSeconds"/> and updates <see cref="Volume"/> and <see cref="Pitch"/>.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (IsAudible)
            {
                presence = Step(presence, 1f, deltaSeconds, returningFromHush ? envelope.HushFadeInSeconds : envelope.FadeInSeconds);
                returningFromHush &= presence < 1f;
            }
            else
            {
                presence = Step(presence, 0f, deltaSeconds, hushed ? envelope.HushFadeOutSeconds : envelope.FadeOutSeconds);
                returningFromHush &= hushed;
            }

            phaseElapsedSeconds += deltaSeconds;
            while (true)
            {
                switch (phase)
                {
                    case SwellPhase.Attack:
                        if (Ramp(swellFrom, 1f, pitchFrom, swellTargetPitch, envelope.SwellAttackSeconds))
                        {
                            phase = SwellPhase.Hold;
                            continue;
                        }

                        return;
                    case SwellPhase.Hold:
                        swell = 1f;
                        Pitch = swellTargetPitch;
                        if (phaseElapsedSeconds >= envelope.SwellHoldSeconds)
                        {
                            phaseElapsedSeconds -= envelope.SwellHoldSeconds;
                            phase = SwellPhase.Release;
                            continue;
                        }

                        return;
                    case SwellPhase.Release:
                        if (Ramp(1f, 0f, swellTargetPitch, NormalPitch, envelope.SwellReleaseSeconds))
                        {
                            phase = SwellPhase.Resting;
                        }

                        return;
                    default:
                        swell = 0f;
                        Pitch = NormalPitch;
                        return;
                }
            }
        }

        // Moves the swell and pitch along a phase; true once the phase is over, with the time past its end carried on.
        private bool Ramp(float fromSwell, float toSwell, float fromPitch, float toPitch, float seconds)
        {
            if (phaseElapsedSeconds >= seconds)
            {
                swell = toSwell;
                Pitch = toPitch;
                phaseElapsedSeconds -= seconds;
                return true;
            }

            var t = phaseElapsedSeconds / seconds;
            swell = fromSwell + (toSwell - fromSwell) * t;
            Pitch = fromPitch + (toPitch - fromPitch) * t;
            return false;
        }

        private static float Step(float value, float target, float deltaSeconds, float fullSweepSeconds)
        {
            if (fullSweepSeconds <= 0f)
            {
                return target;
            }

            var step = deltaSeconds / fullSweepSeconds;
            return value < target ? Math.Min(target, value + step) : Math.Max(target, value - step);
        }
    }
}
