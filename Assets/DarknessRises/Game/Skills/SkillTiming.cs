using System;

namespace DarknessRises.Game.Skills
{
    /// <summary>How long a Skill lasts once triggered, and how long its cooldown runs after it ends.</summary>
    public readonly struct SkillTiming
    {
        public SkillTiming(float durationSeconds, float cooldownSeconds)
        {
            if (durationSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), durationSeconds, "A Skill must last some time.");
            }

            if (cooldownSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cooldownSeconds), cooldownSeconds, "A cooldown can't be negative.");
            }

            DurationSeconds = durationSeconds;
            CooldownSeconds = cooldownSeconds;
        }

        /// <summary>Seconds the Skill's effect lasts.</summary>
        public float DurationSeconds { get; }

        /// <summary>Real-time seconds from the end of the effect until the Skill can be triggered again.</summary>
        public float CooldownSeconds { get; }
    }
}
