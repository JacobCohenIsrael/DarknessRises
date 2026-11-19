using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarknessRises.Game.Scoring
{
    /// <summary>
    /// Authored rules of the Score: how the Combo builds and ends, and what each Grade of a Station's fight requires.
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

        public IReadOnlyList<GradeRequirement> Grades => grades;
        public Grade LowestGrade => lowestGrade;
        public int ComboMaxMultiplier => comboMaxMultiplier;

        public Combo ToCombo() => new(comboHitsPerStep, comboMaxMultiplier, comboTimeoutSeconds);

        public ScoreKeeper ToKeeper() => new(ToCombo(), grades, lowestGrade);
    }
}
