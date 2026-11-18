using System;

namespace DarknessRises.Game.Runs
{
    /// <summary>
    /// The Ramp at one Level index: how much bigger its Waves are, how many extra Waves each Station gets, how much
    /// tougher and faster its Enemies are, what share of each Wave is Runners, and how much tougher its Boss is.
    /// Made by <see cref="Ramp{TEnemyType}.StepAt"/>.
    /// </summary>
    public readonly struct RampStep
    {
        public RampStep(
            int levelIndex,
            float waveSizeMultiplier,
            int extraWaves,
            float enemyHitPointsMultiplier,
            float enemySpeedMultiplier,
            float runnerShare,
            float bossHitPointsMultiplier)
        {
            LevelIndex = levelIndex;
            WaveSizeMultiplier = waveSizeMultiplier;
            ExtraWaves = extraWaves;
            EnemyHitPointsMultiplier = enemyHitPointsMultiplier;
            EnemySpeedMultiplier = enemySpeedMultiplier;
            RunnerShare = runnerShare;
            BossHitPointsMultiplier = bossHitPointsMultiplier;
        }

        /// <summary>The first Level: everything as authored.</summary>
        public static RampStep First => new(RunRecord.FirstLevel, 1f, 0, 1f, 1f, 0f, 1f);

        public int LevelIndex { get; }

        /// <summary>Each Wave's size, as a multiple of its authored size.</summary>
        public float WaveSizeMultiplier { get; }

        /// <summary>Waves added to each Station that has any, repeating its last Wave.</summary>
        public int ExtraWaves { get; }

        public float EnemyHitPointsMultiplier { get; }

        public float EnemySpeedMultiplier { get; }

        /// <summary>The share of each Wave's Enemies that are Runners, from 0 to 1.</summary>
        public float RunnerShare { get; }

        /// <summary>The Boss's hit points, as a multiple of its authored hit points.</summary>
        public float BossHitPointsMultiplier { get; }

        public bool HasRunners => RunnerShare > 0f;

        /// <summary>The hit points of an Enemy authored with <paramref name="authoredHitPoints"/>, at this step. Never below 1.</summary>
        public int EnemyHitPoints(int authoredHitPoints) => Scale(authoredHitPoints, EnemyHitPointsMultiplier);

        /// <summary>The speed of an Enemy authored with <paramref name="authoredSpeed"/>, at this step.</summary>
        public float EnemySpeed(float authoredSpeed) => authoredSpeed * EnemySpeedMultiplier;

        /// <summary>The hit points of a Boss authored with <paramref name="authoredHitPoints"/>, at this step. Never below 1.</summary>
        public int BossHitPoints(int authoredHitPoints) => Scale(authoredHitPoints, BossHitPointsMultiplier);

        private static int Scale(int authored, float multiplier)
        {
            return Math.Max(1, (int)Math.Round(authored * (double)multiplier, MidpointRounding.AwayFromZero));
        }
    }
}
