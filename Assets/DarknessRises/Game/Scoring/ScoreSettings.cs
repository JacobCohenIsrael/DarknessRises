using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarknessRises.Game.Scoring
{
    /// <summary>
    /// Authored rules of the Score: how the Combo builds and ends, what each Grade of a Station's fight requires, the
    /// accuracy bonus on each Station's Score, and the Score boost a rewarded ad starts.
    /// What a kill is worth is authored on each Enemy type and Boss.
    /// </summary>
    [Serializable]
    public sealed class ScoreSettings
    {
        [Header("Combo")]
        [SerializeField, Min(1), Tooltip("Consecutive hits it takes to raise the Combo's multiplier by one.")]
        private int comboHitsPerStep = 4;

        [SerializeField, Min(1), Tooltip("The highest the Combo's multiplier goes. 1 switches the Combo off.")]
        private int comboMaxMultiplier = 5;

        [SerializeField, Min(0f), Tooltip("Seconds without a hit after which the Combo ends. A miss ends it at once. 0 never times out.")]
        private float comboTimeoutSeconds = 3f;

        [Header("Grades")]
        [SerializeField, Tooltip(
            "What a Station's fight must manage for each Grade. The fight earns the best Grade it meets, and the lowest Grade below when it meets none.")]
        private GradeRequirement[] grades =
        {
            new(Grade.S, 0.8f, 6f, 0),
            new(Grade.A, 0.65f, 9f, 1),
            new(Grade.B, 0.45f, 0f, -1),
        };

        [SerializeField, Tooltip("The Grade of a fight that meets none of the requirements above.")]
        private Grade lowestGrade = Grade.C;

        [Header("Accuracy bonus")]
        [SerializeField, Tooltip(
            "What each Station's Score is multiplied by from its fight's accuracy: the best tier the accuracy reaches. "
            + "The steps sit on the Grade lines (B 45 %, A 65 %, S 80 %) so the bonus and the letter agree; a fight "
            + "under 45 % gets x1. A perfect fight doubles its Score, the most any fight can earn.")]
        private AccuracyTier[] accuracyTiers =
        {
            new(0.45f, 1.1f),
            new(0.65f, 1.25f),
            new(0.8f, 1.5f),
            new(0.9f, 1.75f),
            new(1f, 2f),
        };

        [SerializeField, Min(0), Tooltip(
            "The fewest Shots (hits and misses) a fight needs for its accuracy to earn a bonus, so one lucky Shot isn't "
            + "a perfect fight. Fewer gets x1, and so does a fight won without Shots (Grenades) or with only Critter kills.")]
        private int accuracyMinShots = 5;

        [Header("Score boost")]
        [SerializeField, Min(1), Tooltip("What every Score is multiplied by while the boost runs: kills, and so each Station's accuracy bonus. 2 doubles it.")]
        private int boostFactor = 2;

        [SerializeField, Min(1f), Tooltip(
            "Seconds of play one boost lasts. Its timer stands still while paused, while an ad plays, on loading screens "
            + "and while the character is dead, and it carries over from Level to Level until it's up or the Run ends.")]
        private float boostSeconds = 60f;

        [SerializeField, Min(0), Tooltip(
            "How many boosts a Run may have. 0: no limit beyond the player's rule that no card offers one while one runs.")]
        private int boostsPerRun;

        [SerializeField, Min(0f), Tooltip("Seconds the placeholder ad for the boost plays, until a real ad network replaces it.")]
        private float boostFakeAdSeconds = 5f;

        public IReadOnlyList<GradeRequirement> Grades => grades;
        public Grade LowestGrade => lowestGrade;
        public int ComboMaxMultiplier => comboMaxMultiplier;

        public Combo ToCombo() => new(comboHitsPerStep, comboMaxMultiplier, comboTimeoutSeconds);

        public float BoostFakeAdSeconds => boostFakeAdSeconds;

        public AccuracyBonus ToAccuracyBonus() => new(accuracyTiers, accuracyMinShots);

        public ScoreBoost ToBoost() => new(boostFactor, boostSeconds, boostsPerRun);

        public ScoreKeeper ToKeeper() => new(ToCombo(), grades, lowestGrade);

        /// <summary>A keeper with the accuracy bonus, whose kills <paramref name="boost"/> multiplies while it runs.</summary>
        public ScoreKeeper ToKeeper(ScoreBoost boost) => new(ToCombo(), grades, lowestGrade, ToAccuracyBonus(), boost);
    }
}
