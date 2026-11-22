using System;
using UnityEngine;

namespace DarknessRises.Game.Credits
{
    /// <summary>Authored rate of <see cref="ScoreToCredits"/>: how much of a Run's Score pays one Credit.</summary>
    [Serializable]
    public sealed class ScoreToCreditsSettings
    {
        [SerializeField, Min(1), Tooltip(
            "The Score that pays one Credit, rounded down over the Run's whole Score. Tuned so an average Run pays about what "
            + "the old per-kill Credits did, and a strong Run (long Combos, good accuracy, the x2 Score boost) several times more. "
            + "The balance table is in PR for issue #127.")]
        private int scorePerCredit = 35;

        public int ScorePerCredit => scorePerCredit;

        /// <summary>The conversion rule, with this rate.</summary>
        public ScoreToCredits ToRule() => new(scorePerCredit);
    }
}
