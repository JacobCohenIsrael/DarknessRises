using DarknessRises.Game.Enemies;
using UnityEngine;

namespace DarknessRises.Game.Runs
{
    /// <summary>
    /// Authored tuning for the Ramp: how each Level after the first grows its Waves and toughens and speeds up its
    /// Enemies and Boss, and from which Level Runners join the Waves. Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Ramp Definition", fileName = "RampDefinition", order = 45)]
    public sealed class RampDefinition : ScriptableObject
    {
        [Header("Waves")]
        [SerializeField, Min(0f), Tooltip("Extra share of each Wave's Enemies per Level after the first. 0.25 means a quarter more each Level.")]
        private float waveSizeGrowthPerLevel = 0.25f;

        [SerializeField, Min(1f), Tooltip("The most a Wave grows to, as a multiple of its authored size.")]
        private float maxWaveSizeMultiplier = 3f;

        [SerializeField, Min(0), Tooltip("Every this many Levels, each Station gets one more Wave (a repeat of its last). 0 means never.")]
        private int levelsPerExtraWave = 3;

        [SerializeField, Min(0), Tooltip("The most extra Waves a Station gets.")]
        private int maxExtraWaves = 2;

        [Header("Enemies")]
        [SerializeField, Min(0f), Tooltip("Extra share of every Enemy's hit points per Level after the first.")]
        private float enemyHitPointsGrowthPerLevel = 0.2f;

        [SerializeField, Min(1f), Tooltip("The most an Enemy's hit points grow to, as a multiple of its authored hit points.")]
        private float maxEnemyHitPointsMultiplier = 4f;

        [SerializeField, Min(0f), Tooltip("Extra share of every Enemy's speed per Level after the first.")]
        private float enemySpeedGrowthPerLevel = 0.06f;

        [SerializeField, Min(1f), Tooltip(
            "The most an Enemy's speed grows to, as a multiple of its authored speed. " +
            "Keep the Shambler's top speed below the character's walking speed.")]
        private float maxEnemySpeedMultiplier = 1.4f;

        [Header("Runners")]
        [SerializeField, Tooltip("The fast, fragile Enemy type that joins the Waves partway into the Ramp.")]
        private EnemyDefinition runner;

        [SerializeField, Min(0), Tooltip("The first Level whose Waves hold Runners. 0 means never.")]
        private int runnersFromLevel = 3;

        [SerializeField, Range(0f, 1f), Tooltip("The share of each Wave that is Runners on that first Level. A Wave gets at least one.")]
        private float firstRunnerShare = 0.2f;

        [SerializeField, Min(0f), Tooltip("How much the Runners' share grows each Level after that.")]
        private float runnerShareGrowthPerLevel = 0.08f;

        [SerializeField, Range(0f, 1f), Tooltip("The largest share of a Wave that can be Runners.")]
        private float maxRunnerShare = 0.5f;

        [Header("Boss")]
        [SerializeField, Min(0f), Tooltip("Extra share of the Boss's hit points per Level after the first.")]
        private float bossHitPointsGrowthPerLevel = 0.35f;

        [Header("Debug")]
        [SerializeField, Min(1), Tooltip("The Level the Debug/JumpToRampLevel key jumps to. The key exists in the Editor and debug builds only.")]
        private int debugJumpLevel = 5;

        public EnemyDefinition Runner => runner;
        public int DebugJumpLevel => debugJumpLevel;

        /// <summary>The Ramp's rules, with this tuning and its Runner.</summary>
        public Ramp<EnemyDefinition> ToRamp()
        {
            var tuning = new RampTuning(
                waveSizeGrowthPerLevel,
                maxWaveSizeMultiplier,
                levelsPerExtraWave,
                maxExtraWaves,
                enemyHitPointsGrowthPerLevel,
                maxEnemyHitPointsMultiplier,
                enemySpeedGrowthPerLevel,
                maxEnemySpeedMultiplier,
                runnersFromLevel,
                firstRunnerShare,
                runnerShareGrowthPerLevel,
                Mathf.Max(firstRunnerShare, maxRunnerShare),
                bossHitPointsGrowthPerLevel);
            return new Ramp<EnemyDefinition>(tuning, runner);
        }
    }
}
