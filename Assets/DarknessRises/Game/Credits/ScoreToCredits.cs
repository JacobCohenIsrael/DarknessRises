using System;

namespace DarknessRises.Game.Credits
{
    /// <summary>
    /// How a Run's Score pays Credits: every whole <see cref="ScorePerCredit"/> points of the Run's Score is one Credit,
    /// rounded down. The conversion is of the Run's whole Score so far, never of each kill on its own, so the points left
    /// over from one kill count towards the next Credit and nothing is lost to rounding. Plain C# (ADR 0003).
    /// </summary>
    public readonly struct ScoreToCredits
    {
        /// <summary>One point of Score is one Credit. For tests and anything that has no authored rate.</summary>
        public static readonly ScoreToCredits OnePerPoint = new(1);

        private readonly int scorePerCredit;

        /// <param name="scorePerCredit">The Score that pays one Credit. At least 1.</param>
        public ScoreToCredits(int scorePerCredit)
        {
            if (scorePerCredit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(scorePerCredit), scorePerCredit, "A Credit costs at least one point of Score.");
            }

            this.scorePerCredit = scorePerCredit;
        }

        /// <summary>The Score that pays one Credit. A default-made rule counts as one point per Credit.</summary>
        public int ScorePerCredit => scorePerCredit < 1 ? 1 : scorePerCredit;

        /// <summary>The Credits a Run's Score of <paramref name="score"/> pays, rounded down.</summary>
        public int CreditsFor(int score)
        {
            if (score < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(score), score, "A Score is never below 0.");
            }

            return score / ScorePerCredit;
        }
    }
}
