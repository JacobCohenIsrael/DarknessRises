using DarknessRises.Game.Enemies;

namespace DarknessRises.Game.Skills
{
    /// <summary>
    /// The character's Skills, the Flare, Adrenaline and the Lure, ticked together with real time. Plain C# (ADR 0003).
    /// </summary>
    public sealed class CharacterSkills
    {
        /// <param name="flareTiming">The Flare's duration and cooldown for this Run, Upgrades applied.</param>
        /// <param name="adrenalineTiming">Adrenaline's duration and cooldown for this Run, Upgrades applied.</param>
        /// <param name="lureTiming">The Lure's time on the floor and cooldown for this Run, Upgrades applied.</param>
        public CharacterSkills(
            FlareDefinition flare,
            AdrenalineDefinition adrenaline,
            LureDefinition lure,
            SkillTiming flareTiming,
            SkillTiming adrenalineTiming,
            SkillTiming lureTiming,
            EnemyTime enemyTime)
        {
            Flare = new Flare(flareTiming, flare.FadeInSeconds, flare.FadeOutSeconds);
            Adrenaline = new Adrenaline(adrenalineTiming, adrenaline.EnemyTimeScale, adrenaline.EaseSeconds, enemyTime);
            Lure = new Lure(lureTiming, lure.Radius, lure.FlightSeconds);
        }

        public Flare Flare { get; }
        public Adrenaline Adrenaline { get; }
        public Lure Lure { get; }

        /// <summary>
        /// Advances every Skill by <paramref name="realDeltaSeconds"/>: frame time at the global time scale, which
        /// Adrenaline never changes, so cooldowns run at full speed while Enemies are slowed.
        /// </summary>
        public void Tick(float realDeltaSeconds)
        {
            Flare.Tick(realDeltaSeconds);
            Adrenaline.Tick(realDeltaSeconds);
            Lure.Tick(realDeltaSeconds);
        }
    }
}
