using System;
using DarknessRises.Game.Skills;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// What one Skill button shows, frame by frame: the ring of the effect running out while active, the cooldown
    /// sweep and its whole seconds while it recharges, and a short pulse the moment it's ready again.
    /// Plain C# (ADR 0003): it reads the <see cref="Skill"/> and is ticked with real time, like the Skill.
    /// </summary>
    public sealed class SkillButtonFace
    {
        private readonly Skill skill;
        private readonly float pulseSeconds;
        private readonly float pulseScale;
        private SkillPhase lastPhase;
        private float pulseSecondsLeft;

        /// <param name="pulseSeconds">Seconds the ready pulse lasts. Zero for none.</param>
        /// <param name="pulseScale">How many times its size the button swells to at the pulse's peak.</param>
        public SkillButtonFace(Skill skill, float pulseSeconds, float pulseScale)
        {
            if (pulseSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(pulseSeconds), pulseSeconds, "A pulse can't last negative time.");
            }

            if (pulseScale < 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(pulseScale), pulseScale, "The pulse swells the button, never shrinks it.");
            }

            this.skill = skill ?? throw new ArgumentNullException(nameof(skill));
            this.pulseSeconds = pulseSeconds;
            this.pulseScale = pulseScale;
            lastPhase = skill.Phase;
        }

        public SkillPhase Phase => skill.Phase;

        /// <summary>The share of the cooldown left, from 1 (just started) to 0; zero unless cooling down.</summary>
        public float SweepFill => skill.Phase == SkillPhase.CoolingDown ? Clamp01(skill.CooldownFraction) : 0f;

        /// <summary>The share of the effect left, from 1 (just triggered) to 0; zero unless active.</summary>
        public float ActiveFill => skill.Phase == SkillPhase.Active ? Clamp01(skill.ActiveFraction) : 0f;

        /// <summary>Whole seconds of cooldown left, rounded up so it reads 1 until the very end; zero unless cooling down.</summary>
        public int SecondsLeft => skill.Phase == SkillPhase.CoolingDown ? (int)Math.Ceiling(skill.CooldownSecondsLeft) : 0;

        /// <summary>The button's scale: 1, swelling and settling back once over the pulse after the Skill is ready again.</summary>
        public float Scale
        {
            get
            {
                if (pulseSecondsLeft <= 0f)
                {
                    return 1f;
                }

                var progress = 1f - pulseSecondsLeft / pulseSeconds;
                return 1f + (pulseScale - 1f) * (float)Math.Sin(Math.PI * progress);
            }
        }

        public bool IsPulsing => pulseSecondsLeft > 0f;

        /// <summary>
        /// Advances the pulse by <paramref name="realDeltaSeconds"/>, after the Skill has been ticked this frame.
        /// A Skill that became ready since the last tick starts the pulse.
        /// </summary>
        public void Tick(float realDeltaSeconds)
        {
            if (realDeltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(realDeltaSeconds), realDeltaSeconds, "Time can't run backwards.");
            }

            pulseSecondsLeft = Math.Max(0f, pulseSecondsLeft - realDeltaSeconds);

            var phase = skill.Phase;
            if (phase == lastPhase)
            {
                return;
            }

            // Ready again, even straight from active with no cooldown at all; triggering cuts the pulse short.
            pulseSecondsLeft = phase == SkillPhase.Ready ? pulseSeconds : 0f;

            lastPhase = phase;
        }

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
