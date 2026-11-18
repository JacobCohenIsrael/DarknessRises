using System;

namespace DarknessRises.Game.Runs
{
    /// <summary>
    /// What the end screen shows about a Run: the Level reached and the Enemies killed. There is no score.
    /// Plain C# (ADR 0003). The <see cref="RunFlow"/> keeps one per Run and moves it on.
    /// </summary>
    public sealed class RunRecord
    {
        public const int FirstLevel = 1;

        public int LevelReached { get; private set; } = FirstLevel;

        public int EnemiesKilled { get; private set; }

        public void CountKill()
        {
            EnemiesKilled++;
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
