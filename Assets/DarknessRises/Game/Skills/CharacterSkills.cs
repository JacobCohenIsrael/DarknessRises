using DarknessRises.Game.Enemies;

namespace DarknessRises.Game.Skills
{
    /// <summary>
    /// The character's Skills, Flare and Slow-mo, ticked together with real time. Plain C# (ADR 0003).
    /// </summary>
    public sealed class CharacterSkills
    {
        public CharacterSkills(FlareDefinition flare, SlowMoDefinition slowMo, EnemyTime enemyTime)
        {
            Flare = new Flare(flare.Timing, flare.FadeInSeconds, flare.FadeOutSeconds);
            SlowMo = new SlowMo(slowMo.Timing, slowMo.EnemyTimeScale, slowMo.EaseSeconds, enemyTime);
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
