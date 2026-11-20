using System;
using DarknessRises.Game.Runs;

namespace DarknessRises.Game.Credits
{
    /// <summary>
    /// The Credits a cleared Level pays on top of its kills: an authored amount for the first Level that grows with every
    /// Level after it, up to a cap. Like the Ramp, it's a rule of the Level index. Plain C# (ADR 0003).
    /// </summary>
    public readonly struct LevelClearBonus
    {
        private readonly int firstLevelCredits;
        private readonly int growthPerLevel;
        private readonly int maxCredits;

        /// <param name="firstLevelCredits">What clearing Level 1 pays.</param>
        /// <param name="growthPerLevel">How much more each Level after the first pays.</param>
        /// <param name="maxCredits">The most a cleared Level pays. Never below the first Level's bonus.</param>
        public LevelClearBonus(int firstLevelCredits, int growthPerLevel, int maxCredits)
        {
            if (firstLevelCredits < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(firstLevelCredits), firstLevelCredits, "A cleared Level never costs Credits.");
            }

            if (growthPerLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(growthPerLevel), growthPerLevel, "Later Levels never pay less.");
            }

            this.firstLevelCredits = firstLevelCredits;
            this.growthPerLevel = growthPerLevel;
            this.maxCredits = Math.Max(firstLevelCredits, maxCredits);
        }

        /// <summary>What clearing Level <paramref name="levelIndex"/> (counted from 1) pays.</summary>
        public int For(int levelIndex)
        {
            if (levelIndex < RunRecord.FirstLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(levelIndex), levelIndex, "Levels count from 1.");
            }

            // In long arithmetic, so a Level far along an endless Run can't overflow before the cap applies.
            var levelsIn = levelIndex - RunRecord.FirstLevel;
            return (int)Math.Min(maxCredits, firstLevelCredits + (long)growthPerLevel * levelsIn);
        }
    }
}
