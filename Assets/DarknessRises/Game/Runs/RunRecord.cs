using System;
using DarknessRises.Game.Credits;

namespace DarknessRises.Game.Runs
{
    /// <summary>
    /// What the end screen shows about a Run: the Level reached, the Enemies killed, the Score and the Credits it earned.
    /// Plain C# (ADR 0003). The <see cref="RunFlow"/> keeps one per Run and moves it on.
    /// </summary>
    public sealed class RunRecord
    {
        public const int FirstLevel = 1;

        /// <summary>A Run whose every point of Score is one Credit. Tests use it to earn exact amounts.</summary>
        public RunRecord()
            : this(ScoreToCredits.OnePerPoint)
        {
        }

        /// <param name="scoreToCredits">How the Run's Score pays Credits.</param>
        public RunRecord(ScoreToCredits scoreToCredits)
        {
            Credits = new RunCredits(scoreToCredits);
        }

        public int LevelReached { get; private set; } = FirstLevel;

        public int EnemiesKilled { get; private set; }

        /// <summary>
        /// The Run's Score: the points of every kill, multiplied by the Combo at the time, and each Station's accuracy bonus.
        /// It pays the Run's <see cref="Credits"/>.
        /// </summary>
        public int Score { get; private set; }

        /// <summary>
        /// The best Score of any Run before this one ended, once it has ended and been compared
        /// (<see cref="CompareWithBest"/>); 0 until then.
        /// </summary>
        public int BestScoreBefore { get; private set; }

        /// <summary>Whether this Run has been compared with the best Score yet.</summary>
        public bool IsComparedWithBest { get; private set; }

        /// <summary>Whether this Run's Score beat the best of every Run before it. A Run that scored nothing never does.</summary>
        public bool IsNewBest => IsComparedWithBest && Score > 0 && Score > BestScoreBefore;

        /// <summary>The best Score with this Run counted.</summary>
        public int BestScore => Math.Max(Score, BestScoreBefore);

        /// <summary>The Credits the Run earned (from its Score and its cleared Levels), and how many of them are banked.</summary>
        public RunCredits Credits { get; }

        public void CountKill()
        {
            EnemiesKilled++;
        }

        /// <summary>Adds <paramref name="points"/> to the Run's Score.</summary>
        public void AddScore(int points)
        {
            if (points < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(points), points, "Score is only ever earned.");
            }

            Score += points;
            Credits.AddScore(points);
        }

        /// <summary>The Run ended: it's compared with <paramref name="bestBefore"/>, the best Score of every Run before it.</summary>
        public void CompareWithBest(int bestBefore)
        {
            if (bestBefore < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bestBefore), bestBefore, "A Score is never below 0.");
            }

            BestScoreBefore = bestBefore;
            IsComparedWithBest = true;
        }

        /// <summary>Records that the Run reached Level <paramref name="levelIndex"/>.</summary>
        public void Reach(int levelIndex)
        {
            if (levelIndex < FirstLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(levelIndex), levelIndex, "Levels count from 1.");
            }

            LevelReached = levelIndex;
        }
    }
}
