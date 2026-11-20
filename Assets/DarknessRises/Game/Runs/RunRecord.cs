using System;

namespace DarknessRises.Game.Runs
{
    /// <summary>
    /// What the end screen shows about a Run: the Level reached, the Enemies killed, the Score and the Credits it earned.
    /// Plain C# (ADR 0003). The <see cref="RunFlow"/> keeps one per Run and moves it on.
    /// </summary>
    public sealed class RunRecord
    {
        public const int FirstLevel = 1;

        public int LevelReached { get; private set; } = FirstLevel;

        public int EnemiesKilled { get; private set; }

        /// <summary>The Run's Score: the points of every kill, multiplied by the Combo at the time. It isn't Credits.</summary>
        public int Score { get; private set; }

        /// <summary>The Credits the Run earned, and how many of them are banked. They aren't the Score.</summary>
        public RunCredits Credits { get; } = new();

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
