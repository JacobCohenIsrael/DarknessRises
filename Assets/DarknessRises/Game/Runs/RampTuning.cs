using System;

namespace DarknessRises.Game.Runs
{
    /// <summary>
    /// The Ramp's numbers, copied out of the <see cref="RampDefinition"/> for <see cref="Ramp{TEnemyType}"/>: how much each
    /// Level after the first grows Waves and toughens and speeds up Enemies, and when Runners join. Every parameter left
    /// out means no rise at all.
    /// </summary>
    public sealed class RampTuning
    {
        /// <param name="waveSizeGrowthPerLevel">Extra share of each Wave's Enemies per Level after the first (0.25 = a quarter more).</param>
        /// <param name="maxWaveSizeMultiplier">The most a Wave can grow to, as a multiple of its authored size.</param>
        /// <param name="levelsPerExtraWave">Every this many Levels, each Station gets one more Wave. 0 means never.</param>
        /// <param name="maxExtraWaves">The most extra Waves a Station gets.</param>
        /// <param name="enemyHitPointsGrowthPerLevel">Extra share of an Enemy's hit points per Level after the first.</param>
        /// <param name="maxEnemyHitPointsMultiplier">The most an Enemy's hit points grow to, as a multiple of its authored hit points.</param>
        /// <param name="enemySpeedGrowthPerLevel">Extra share of an Enemy's speed per Level after the first.</param>
        /// <param name="maxEnemySpeedMultiplier">The most an Enemy's speed grows to, as a multiple of its authored speed.</param>
        /// <param name="runnersFromLevel">The first Level whose Waves hold Runners. 0 means never.</param>
        /// <param name="firstRunnerShare">The share of each Wave that is Runners on that first Level.</param>
        /// <param name="runnerShareGrowthPerLevel">How much that share grows each Level after it.</param>
        /// <param name="maxRunnerShare">The largest share of a Wave that can be Runners.</param>
        /// <param name="bossHitPointsGrowthPerLevel">Extra share of the Boss's hit points per Level after the first.</param>
        public RampTuning(
            float waveSizeGrowthPerLevel = 0f,
            float maxWaveSizeMultiplier = float.PositiveInfinity,
            int levelsPerExtraWave = 0,
            int maxExtraWaves = 0,
            float enemyHitPointsGrowthPerLevel = 0f,
            float maxEnemyHitPointsMultiplier = float.PositiveInfinity,
            float enemySpeedGrowthPerLevel = 0f,
            float maxEnemySpeedMultiplier = float.PositiveInfinity,
            int runnersFromLevel = 0,
            float firstRunnerShare = 0f,
            float runnerShareGrowthPerLevel = 0f,
            float maxRunnerShare = 1f,
            float bossHitPointsGrowthPerLevel = 0f)
        {
            RequireNotNegative(waveSizeGrowthPerLevel, nameof(waveSizeGrowthPerLevel));
            RequireAtLeastOne(maxWaveSizeMultiplier, nameof(maxWaveSizeMultiplier));
            RequireNotNegative(levelsPerExtraWave, nameof(levelsPerExtraWave));
            RequireNotNegative(maxExtraWaves, nameof(maxExtraWaves));
            RequireNotNegative(enemyHitPointsGrowthPerLevel, nameof(enemyHitPointsGrowthPerLevel));
            RequireAtLeastOne(maxEnemyHitPointsMultiplier, nameof(maxEnemyHitPointsMultiplier));
            RequireNotNegative(enemySpeedGrowthPerLevel, nameof(enemySpeedGrowthPerLevel));
            RequireAtLeastOne(maxEnemySpeedMultiplier, nameof(maxEnemySpeedMultiplier));
            RequireNotNegative(runnersFromLevel, nameof(runnersFromLevel));
            RequireShare(firstRunnerShare, nameof(firstRunnerShare));
            RequireNotNegative(runnerShareGrowthPerLevel, nameof(runnerShareGrowthPerLevel));
            RequireShare(maxRunnerShare, nameof(maxRunnerShare));
            RequireNotNegative(bossHitPointsGrowthPerLevel, nameof(bossHitPointsGrowthPerLevel));

            WaveSizeGrowthPerLevel = waveSizeGrowthPerLevel;
            MaxWaveSizeMultiplier = maxWaveSizeMultiplier;
            LevelsPerExtraWave = levelsPerExtraWave;
            MaxExtraWaves = maxExtraWaves;
            EnemyHitPointsGrowthPerLevel = enemyHitPointsGrowthPerLevel;
            MaxEnemyHitPointsMultiplier = maxEnemyHitPointsMultiplier;
            EnemySpeedGrowthPerLevel = enemySpeedGrowthPerLevel;
            MaxEnemySpeedMultiplier = maxEnemySpeedMultiplier;
            RunnersFromLevel = runnersFromLevel;
            FirstRunnerShare = firstRunnerShare;
            RunnerShareGrowthPerLevel = runnerShareGrowthPerLevel;
            MaxRunnerShare = maxRunnerShare;
            BossHitPointsGrowthPerLevel = bossHitPointsGrowthPerLevel;
        }

        public float WaveSizeGrowthPerLevel { get; }
        public float MaxWaveSizeMultiplier { get; }
        public int LevelsPerExtraWave { get; }
        public int MaxExtraWaves { get; }
        public float EnemyHitPointsGrowthPerLevel { get; }
        public float MaxEnemyHitPointsMultiplier { get; }
        public float EnemySpeedGrowthPerLevel { get; }
        public float MaxEnemySpeedMultiplier { get; }
        public int RunnersFromLevel { get; }
        public float FirstRunnerShare { get; }
        public float RunnerShareGrowthPerLevel { get; }
        public float MaxRunnerShare { get; }
        public float BossHitPointsGrowthPerLevel { get; }

        private static void RequireNotNegative(float value, string name)
        {
            if (value < 0f)
            {
                throw new ArgumentOutOfRangeException(name, value, "A Ramp value can't be negative.");
            }
        }

        private static void RequireAtLeastOne(float value, string name)
        {
            if (value < 1f)
            {
                throw new ArgumentOutOfRangeException(name, value, "The Ramp never makes anything weaker than authored.");
            }
        }

        private static void RequireShare(float value, string name)
        {
            if (value < 0f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(name, value, "A share is between 0 and 1.");
            }
        }
    }
}
