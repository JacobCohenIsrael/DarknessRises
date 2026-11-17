using System;
using DarknessRises.Game.Enemies;
using UnityEngine;

namespace DarknessRises.Game.Levels
{
    /// <summary>A Room's Stragglers: lone Enemies that come at the character while it walks between Stations.</summary>
    [Serializable]
    public sealed class StragglerSettings
    {
        [SerializeField, Tooltip("The Enemy type that comes. None means the Room has no Stragglers.")]
        private EnemyDefinition enemyType;

        [SerializeField, Min(0f), Tooltip("Shortest walking time, in seconds, from one Straggler to the next.")]
        private float minIntervalSeconds = 6f;

        [SerializeField, Min(0f), Tooltip("Longest walking time, in seconds, from one Straggler to the next.")]
        private float maxIntervalSeconds = 10f;

        [SerializeField, Min(0), Tooltip("Most Stragglers that may be after the character at once. 0 means none come.")]
        private int maxAlive = 1;

        public EnemyDefinition EnemyType => enemyType;
        public float MinIntervalSeconds => minIntervalSeconds;
        public float MaxIntervalSeconds => maxIntervalSeconds;
        public int MaxAlive => maxAlive;

        /// <summary>A copy for the <see cref="StragglerSchedule{TEnemyType}"/>, or null when the Room has no Stragglers.</summary>
        public StragglerPlan<EnemyDefinition> ToPlan()
        {
            if (enemyType == null || maxAlive == 0)
            {
                return null;
            }

            return new StragglerPlan<EnemyDefinition>(
                enemyType,
                minIntervalSeconds,
                Mathf.Max(minIntervalSeconds, maxIntervalSeconds),
                maxAlive);
        }
    }
}
