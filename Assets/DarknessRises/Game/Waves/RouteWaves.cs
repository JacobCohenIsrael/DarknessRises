using System;
using System.Collections.Generic;
using DarknessRises.Game.Routes;

namespace DarknessRises.Game.Waves
{
    /// <summary>
    /// Runs each Station's Waves when the character reaches it, so the Station is cleared once they're dead
    /// and the character walks on. A Station without authored Waves is cleared at once.
    /// </summary>
    public sealed class RouteWaves<TEnemyType> : IDisposable
    {
        private readonly RouteProgress progress;
        private readonly WaveSchedule<TEnemyType>[] schedules;
        private WaveSchedule<TEnemyType> current;

        /// <param name="progress">The character's walk. Its Stations are matched to <paramref name="stationWaves"/> by index.</param>
        /// <param name="stationWaves">Each Station's Waves, by Station index.</param>
        /// <param name="spawner">Brings the Enemies in.</param>
        public RouteWaves(
            RouteProgress progress,
            IReadOnlyList<IReadOnlyList<WavePlan<TEnemyType>>> stationWaves,
            IEnemySpawner<TEnemyType> spawner)
        {
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));

            if (stationWaves == null)
            {
                throw new ArgumentNullException(nameof(stationWaves));
            }

            // Built up front so reaching a Station allocates nothing.
            schedules = new WaveSchedule<TEnemyType>[stationWaves.Count];
            for (var i = 0; i < stationWaves.Count; i++)
            {
                schedules[i] = new WaveSchedule<TEnemyType>(stationWaves[i], spawner);
            }

            progress.StationReached += OnStationReached;
        }

        /// <summary>The fight at the Station the character is at, or null while it walks.</summary>
        public WaveSchedule<TEnemyType> Current => current;

        public void Tick(float deltaSeconds)
        {
            if (current == null)
            {
                return;
            }

            current.Tick(deltaSeconds);
            if (current.IsDone)
            {
                current = null;
            }
        }

        public void Dispose()
        {
            progress.StationReached -= OnStationReached;
            current = null;
        }

        private void OnStationReached(Station station)
        {
            if (station.Index >= schedules.Length)
            {
                station.Clear();
                return;
            }

            var schedule = schedules[station.Index];
            schedule.Begin(station);
            current = schedule.IsDone ? null : schedule;
        }
    }
}
