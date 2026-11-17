using System;

namespace DarknessRises.Game.Levels
{
    /// <summary>
    /// A Room's Stragglers, copied out of its definition for the <see cref="StragglerSchedule{TEnemyType}"/>:
    /// which Enemy type comes, how far apart, and how many may be after the character at once.
    /// </summary>
    public sealed class StragglerPlan<TEnemyType>
    {
        public StragglerPlan(TEnemyType enemyType, float minIntervalSeconds, float maxIntervalSeconds, int maxAlive)
        {
            if (minIntervalSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(minIntervalSeconds), minIntervalSeconds, "A Straggler interval can't be negative.");
            }

            if (maxIntervalSeconds < minIntervalSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(maxIntervalSeconds), maxIntervalSeconds, "The longest Straggler interval can't be shorter than the shortest.");
            }

            if (maxAlive < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAlive), maxAlive, "The number of Stragglers at once can't be negative.");
            }

            EnemyType = enemyType;
            MinIntervalSeconds = minIntervalSeconds;
            MaxIntervalSeconds = maxIntervalSeconds;
            MaxAlive = maxAlive;
        }

        public TEnemyType EnemyType { get; }

        /// <summary>Shortest walking time, in seconds, from one Straggler to the next.</summary>
        public float MinIntervalSeconds { get; }

        /// <summary>Longest walking time, in seconds, from one Straggler to the next.</summary>
        public float MaxIntervalSeconds { get; }

        /// <summary>Most Stragglers that may be alive at once. No more come until one dies.</summary>
        public int MaxAlive { get; }
    }
}
