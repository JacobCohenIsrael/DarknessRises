using System;
using UnityEngine;

namespace DarknessRises.Game.Credits
{
    /// <summary>Authored numbers of the <see cref="LevelClearBonus"/>: the Credits a cleared Level pays, by its Level index.</summary>
    [Serializable]
    public sealed class LevelClearBonusSettings
    {
        [SerializeField, Min(0), Tooltip("Credits for clearing Level 1.")]
        private int firstLevelCredits = 100;

        [SerializeField, Min(0), Tooltip("Extra Credits for each Level after the first: Level 3 pays the first Level's bonus plus twice this.")]
        private int growthPerLevel = 50;

        [SerializeField, Min(0), Tooltip("The most a cleared Level pays, however far the Run gets.")]
        private int maxCredits = 500;

        /// <summary>The bonus rule, with these numbers.</summary>
        public LevelClearBonus ToBonus() => new(firstLevelCredits, growthPerLevel, maxCredits);
    }
}
