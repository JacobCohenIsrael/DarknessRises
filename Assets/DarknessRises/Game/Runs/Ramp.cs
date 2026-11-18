using System;
using System.Collections.Generic;
using DarknessRises.Game.Waves;

namespace DarknessRises.Game.Runs
{
    /// <summary>
    /// The rise in difficulty from Level to Level: given a Level index, the <see cref="RampStep"/> that scales Enemy
    /// stats and the Boss, and the scaled Waves of each Station. Waves grow, Stations get extra Waves, and from a
    /// configured Level a growing share of each Wave is Runners. Plain C# (ADR 0003).
    /// </summary>
    public sealed class Ramp<TEnemyType>
    {
        private readonly RampTuning tuning;
        private readonly bool hasRunner;

        /// <param name="runner">The Enemy type that joins the Waves from the configured Level. With none, no Runners come.</param>
        public Ramp(RampTuning tuning, TEnemyType runner)
        {
            this.tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            Runner = runner;
            hasRunner = !EqualityComparer<TEnemyType>.Default.Equals(runner, default);
        }

        public TEnemyType Runner { get; }

        /// <summary>The Ramp at Level <paramref name="levelIndex"/>, counted from 1.</summary>
        public RampStep StepAt(int levelIndex)
        {
            if (levelIndex < RunRecord.FirstLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(levelIndex), levelIndex, "Levels count from 1.");
            }

            var levelsIn = levelIndex - RunRecord.FirstLevel;
            var extraWaves = tuning.LevelsPerExtraWave > 0
                ? Math.Min(tuning.MaxExtraWaves, levelsIn / tuning.LevelsPerExtraWave)
                : 0;

            return new RampStep(
                levelIndex,
                Grow(tuning.WaveSizeGrowthPerLevel, tuning.MaxWaveSizeMultiplier, levelsIn),
                extraWaves,
                Grow(tuning.EnemyHitPointsGrowthPerLevel, tuning.MaxEnemyHitPointsMultiplier, levelsIn),
                Grow(tuning.EnemySpeedGrowthPerLevel, tuning.MaxEnemySpeedMultiplier, levelsIn),
                RunnerShareAt(levelIndex),
                Grow(tuning.BossHitPointsGrowthPerLevel, float.PositiveInfinity, levelsIn));
        }

        /// <summary>
        /// A Station's Waves at <paramref name="step"/>: each Wave scaled by <see cref="ScaleWave"/>, then
        /// <see cref="RampStep.ExtraWaves"/> more, each a repeat of the last. A Station without Waves stays without.
        /// </summary>
        public WavePlan<TEnemyType>[] ScaleStation(IReadOnlyList<WavePlan<TEnemyType>> waves, RampStep step)
        {
            if (waves == null)
            {
                throw new ArgumentNullException(nameof(waves));
            }

            if (waves.Count == 0)
            {
                return Array.Empty<WavePlan<TEnemyType>>();
            }

            var scaled = new WavePlan<TEnemyType>[waves.Count + step.ExtraWaves];
            for (var i = 0; i < waves.Count; i++)
            {
                scaled[i] = ScaleWave(waves[i], step);
            }

            for (var i = waves.Count; i < scaled.Length; i++)
            {
                scaled[i] = scaled[waves.Count - 1];
            }

            return scaled;
        }

        /// <summary>
        /// One Wave at <paramref name="step"/>: its authored spawn order repeated until the Wave has grown to its scaled
        /// size, with Runners spread evenly through it in place of the authored Enemies. Its timing stays as authored.
        /// </summary>
        public WavePlan<TEnemyType> ScaleWave(WavePlan<TEnemyType> wave, RampStep step)
        {
            if (wave == null)
            {
                throw new ArgumentNullException(nameof(wave));
            }

            var authored = wave.Count;
            if (authored == 0)
            {
                return wave;
            }

            var size = Math.Max(authored, Round(authored * step.WaveSizeMultiplier));
            var runners = hasRunner && step.HasRunners ? Math.Min(size, Math.Max(1, Round(size * step.RunnerShare))) : 0;
            var order = new TEnemyType[size];
            for (var i = 0; i < size; i++)
            {
                // Evenly spread: the ith Enemy is a Runner whenever the running count of Runners steps up on it.
                var isRunner = (i + 1) * runners / size > i * runners / size;
                order[i] = isRunner ? Runner : wave.EnemyTypeAt(i % authored);
            }

            return new WavePlan<TEnemyType>(wave.DelaySeconds, wave.SpawnIntervalSeconds, order);
        }

        private float RunnerShareAt(int levelIndex)
        {
            if (!hasRunner || tuning.RunnersFromLevel == 0 || levelIndex < tuning.RunnersFromLevel)
            {
                return 0f;
            }

            var share = tuning.FirstRunnerShare + tuning.RunnerShareGrowthPerLevel * (levelIndex - tuning.RunnersFromLevel);
            return Math.Min(tuning.MaxRunnerShare, share);
        }

        private static float Grow(float growthPerLevel, float max, int levelsIn)
        {
            return Math.Min(max, 1f + growthPerLevel * levelsIn);
        }

        private static int Round(float value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
