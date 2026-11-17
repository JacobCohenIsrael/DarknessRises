using System;
using System.Collections.Generic;
using DarknessRises.Game.Enemies;
using UnityEngine;

namespace DarknessRises.Game.Waves
{
    /// <summary>One authored Wave: a group of Enemies that enter together, one after another.</summary>
    [Serializable]
    public sealed class WaveDefinition
    {
        [SerializeField, Min(0f), Tooltip(
            "Seconds before the first Enemy enters, counted from arrival at the Station for the first Wave " +
            "and from the previous Wave's last death for the others.")]
        private float delaySeconds = 1f;

        [SerializeField, Min(0f), Tooltip("Seconds between one Enemy entering and the next.")]
        private float spawnIntervalSeconds = 1f;

        [SerializeField, Tooltip("Who enters, group by group, in order.")]
        private WaveGroup[] groups = Array.Empty<WaveGroup>();

        public float DelaySeconds => delaySeconds;
        public float SpawnIntervalSeconds => spawnIntervalSeconds;
        public IReadOnlyList<WaveGroup> Groups => groups;

        /// <summary>A copy of the Wave for the <see cref="WaveSchedule{TEnemyType}"/>, with its groups laid out in spawn order.</summary>
        public WavePlan<EnemyDefinition> ToPlan()
        {
            var order = new List<EnemyDefinition>();
            foreach (var group in groups)
            {
                if (group.EnemyType == null)
                {
                    continue;
                }

                for (var i = 0; i < group.Count; i++)
                {
                    order.Add(group.EnemyType);
                }
            }

            return new WavePlan<EnemyDefinition>(delaySeconds, spawnIntervalSeconds, order);
        }
    }
}
