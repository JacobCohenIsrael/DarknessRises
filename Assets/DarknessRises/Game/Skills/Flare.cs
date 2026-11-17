using System;

namespace DarknessRises.Game.Skills
{
    /// <summary>
    /// The Flare Skill: while active, the character gives off strong light, fading in quickly at the start and
    /// out at the end. <see cref="Brightness"/> is the light's share of its authored strength right now.
    /// </summary>
    public sealed class Flare
    {
        private readonly float fadeInSeconds;
        private readonly float fadeOutSeconds;

        public Flare(SkillTiming timing, float fadeInSeconds, float fadeOutSeconds)
        {
            if (fadeInSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fadeInSeconds), fadeInSeconds, "A fade can't take negative time.");
            }

            if (fadeOutSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fadeOutSeconds), fadeOutSeconds, "A fade can't take negative time.");
            }

            Skill = new Skill(timing);
            this.fadeInSeconds = fadeInSeconds;
            this.fadeOutSeconds = fadeOutSeconds;
        }

        public Skill Skill { get; }

        /// <summary>From 0 (no light) to 1 (full strength).</summary>
        public float Brightness
        {
            get
            {
                if (!Skill.IsActive)
                {
                    return 0f;
                }

                var fadingIn = fadeInSeconds > 0f ? Skill.ActiveSecondsElapsed / fadeInSeconds : 1f;
                var fadingOut = fadeOutSeconds > 0f ? Skill.ActiveSecondsLeft / fadeOutSeconds : 1f;
                return Math.Min(1f, Math.Min(fadingIn, fadingOut));
            }
        }

        public void Tick(float realDeltaSeconds)
        {
            Skill.Tick(realDeltaSeconds);
        }
    }
}
