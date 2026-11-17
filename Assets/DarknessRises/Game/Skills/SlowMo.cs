using System;
using DarknessRises.Game.Enemies;

namespace DarknessRises.Game.Skills
{
    /// <summary>
    /// The Slow-mo Skill: while active, eases the Enemy time scale down to its authored strength, and back up to
    /// normal once it ends. Never touches the global time scale, so the character, Shots and Skills keep full speed.
    /// </summary>
    public sealed class SlowMo
    {
        private readonly EnemyTime enemyTime;
        private readonly float enemyTimeScale;
        private readonly float easeSeconds;

        public SlowMo(SkillTiming timing, float enemyTimeScale, float easeSeconds, EnemyTime enemyTime)
        {
            if (enemyTimeScale < 0f || enemyTimeScale > EnemyTime.NormalScale)
            {
                throw new ArgumentOutOfRangeException(nameof(enemyTimeScale), enemyTimeScale, "Slow-mo scales Enemy time between 0 and 1.");
            }

            if (easeSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(easeSeconds), easeSeconds, "Easing can't take negative time.");
            }

            Skill = new Skill(timing);
            this.enemyTimeScale = enemyTimeScale;
            this.easeSeconds = easeSeconds;
            this.enemyTime = enemyTime;
        }

        public Skill Skill { get; }

        /// <summary>Advances the Skill and eases the Enemy time scale by <paramref name="realDeltaSeconds"/> of real time.</summary>
        public void Tick(float realDeltaSeconds)
        {
            Skill.Tick(realDeltaSeconds);

            var target = Skill.IsActive ? enemyTimeScale : EnemyTime.NormalScale;
            if (easeSeconds <= 0f)
            {
                enemyTime.Scale = target;
                return;
            }

            var step = (EnemyTime.NormalScale - enemyTimeScale) * realDeltaSeconds / easeSeconds;
            var current = enemyTime.Scale;
            enemyTime.Scale = current < target ? Math.Min(target, current + step) : Math.Max(target, current - step);
        }
    }
}
