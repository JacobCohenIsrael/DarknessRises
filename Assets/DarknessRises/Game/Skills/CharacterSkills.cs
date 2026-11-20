using DarknessRises.Game.Enemies;

namespace DarknessRises.Game.Skills
{
    /// <summary>
    /// The character's Skills, Flare and Slow-mo, ticked together with real time. Plain C# (ADR 0003).
    /// </summary>
    public sealed class CharacterSkills
    {
        /// <param name="flareTiming">The Flare's duration and cooldown for this Run, Upgrades applied.</param>
        /// <param name="slowMoTiming">Slow-mo's duration and cooldown for this Run, Upgrades applied.</param>
        public CharacterSkills(
            FlareDefinition flare, SlowMoDefinition slowMo, SkillTiming flareTiming, SkillTiming slowMoTiming, EnemyTime enemyTime)
        {
            Flare = new Flare(flareTiming, flare.FadeInSeconds, flare.FadeOutSeconds);
            SlowMo = new SlowMo(slowMoTiming, slowMo.EnemyTimeScale, slowMo.EaseSeconds, enemyTime);
        }

        public Flare Flare { get; }
        public SlowMo SlowMo { get; }

        /// <summary>
        /// Advances both Skills by <paramref name="realDeltaSeconds"/>: frame time at the global time scale, which
        /// Slow-mo never changes, so cooldowns run at full speed while Enemies are slowed.
        /// </summary>
        public void Tick(float realDeltaSeconds)
        {
            Flare.Tick(realDeltaSeconds);
            SlowMo.Tick(realDeltaSeconds);
        }
    }
}
