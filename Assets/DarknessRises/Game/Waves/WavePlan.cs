using System;
using System.Collections.Generic;

namespace DarknessRises.Game.Waves
{
    /// <summary>
    /// One Wave, copied out of its definition for the <see cref="WaveSchedule{TEnemyType}"/>: how long it waits,
    /// how far apart its Enemies enter, and which Enemy types enter, in order.
    /// </summary>
    public sealed class WavePlan<TEnemyType>
    {
        private readonly TEnemyType[] spawnOrder;

        public WavePlan(float delaySeconds, float spawnIntervalSeconds, IReadOnlyList<TEnemyType> spawnOrder)
        {
            if (delaySeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(delaySeconds), delaySeconds, "A Wave's delay can't be negative.");
            }

            if (spawnIntervalSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(spawnIntervalSeconds), spawnIntervalSeconds, "A spawn interval can't be negative.");
            }

            if (spawnOrder == null)
            {
                throw new ArgumentNullException(nameof(spawnOrder));
            }

            DelaySeconds = delaySeconds;
            SpawnIntervalSeconds = spawnIntervalSeconds;
            this.spawnOrder = new TEnemyType[spawnOrder.Count];
            for (var i = 0; i < spawnOrder.Count; i++)
            {
                this.spawnOrder[i] = spawnOrder[i];
            }
        }

        /// <summary>Seconds from the moment the Wave is due (arrival, or the previous Wave's last death) until its first Enemy enters.</summary>
        public float DelaySeconds { get; }

        /// <summary>Seconds between one Enemy of the Wave entering and the next.</summary>
        public float SpawnIntervalSeconds { get; }

        /// <summary>How many Enemies the Wave holds.</summary>
        public int Count => spawnOrder.Length;

        /// <summary>The Enemy type of the Wave's <paramref name="index"/>th Enemy to enter.</summary>
        public TEnemyType EnemyTypeAt(int index) => spawnOrder[index];
    }
}
