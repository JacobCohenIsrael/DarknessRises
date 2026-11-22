using System;

namespace DarknessRises.Game.Skills
{
    /// <summary>
    /// One Skill's changing state: ready, active for its duration, then cooling down until it's ready again.
    /// Plain C# (ADR 0003). Ticked with real time, so Adrenaline never slows a cooldown.
    /// </summary>
    public sealed class Skill
    {
        private readonly SkillTiming timing;

        public Skill(SkillTiming timing)
        {
            this.timing = timing;
        }

        /// <summary>Raised when the player triggers the Skill and its effect starts.</summary>
        public event Action<Skill> Activated;

        /// <summary>Raised when the effect runs out and the cooldown starts.</summary>
        public event Action<Skill> Ended;

        /// <summary>Raised when the cooldown runs out and the Skill can be triggered again.</summary>
        public event Action<Skill> Readied;

        public SkillPhase Phase { get; private set; } = SkillPhase.Ready;

        public bool IsReady => Phase == SkillPhase.Ready;
        public bool IsActive => Phase == SkillPhase.Active;
        public SkillTiming Timing => timing;

        /// <summary>Seconds since the effect started. Zero unless active.</summary>
        public float ActiveSecondsElapsed => IsActive ? timing.DurationSeconds - ActiveSecondsLeft : 0f;

        /// <summary>Seconds of effect left. Zero unless active.</summary>
        public float ActiveSecondsLeft { get; private set; }

        /// <summary>Seconds of cooldown left. Zero unless cooling down.</summary>
        public float CooldownSecondsLeft { get; private set; }

        /// <summary>How much of the cooldown is left, from 1 (just started) to 0 (ready).</summary>
        public float CooldownFraction => timing.CooldownSeconds > 0f ? CooldownSecondsLeft / timing.CooldownSeconds : 0f;

        /// <summary>How much of the effect is left, from 1 (just triggered) to 0 (over).</summary>
        public float ActiveFraction => ActiveSecondsLeft / timing.DurationSeconds;

        /// <summary>Starts the effect if the Skill is ready. Returns whether it did.</summary>
        public bool TryActivate()
        {
            if (!IsReady)
            {
                return false;
            }

            Phase = SkillPhase.Active;
            ActiveSecondsLeft = timing.DurationSeconds;
            Activated?.Invoke(this);
            return true;
        }

        /// <summary>
        /// Cuts the effect short (the Lure was broken): the cooldown starts now, as if the effect had run out.
        /// Does nothing unless the Skill is active. Returns whether it ended the effect.
        /// </summary>
        public bool End()
        {
            if (!IsActive)
            {
                return false;
            }

            ActiveSecondsLeft = 0f;
            Phase = SkillPhase.CoolingDown;
            CooldownSecondsLeft = timing.CooldownSeconds;
            Ended?.Invoke(this);
            if (CooldownSecondsLeft <= 0f)
            {
                Phase = SkillPhase.Ready;
                Readied?.Invoke(this);
            }

            return true;
        }

        /// <summary>
        /// Advances the effect and the cooldown by <paramref name="realDeltaSeconds"/> of real time.
        /// Time left over when the effect ends carries into the cooldown.
        /// </summary>
        public void Tick(float realDeltaSeconds)
        {
            if (realDeltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(realDeltaSeconds), realDeltaSeconds, "Time can't run backwards.");
            }

            if (Phase == SkillPhase.Active)
            {
                ActiveSecondsLeft -= realDeltaSeconds;
                if (ActiveSecondsLeft > 0f)
                {
                    return;
                }

                realDeltaSeconds = -ActiveSecondsLeft;
                ActiveSecondsLeft = 0f;
                Phase = SkillPhase.CoolingDown;
                CooldownSecondsLeft = timing.CooldownSeconds;
                Ended?.Invoke(this);
            }

            if (Phase == SkillPhase.CoolingDown)
            {
                CooldownSecondsLeft -= realDeltaSeconds;
                if (CooldownSecondsLeft > 0f)
                {
                    return;
                }

                CooldownSecondsLeft = 0f;
                Phase = SkillPhase.Ready;
                Readied?.Invoke(this);
            }
        }
    }
}
