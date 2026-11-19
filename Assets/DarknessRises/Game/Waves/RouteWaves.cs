using System;
using System.Collections.Generic;
using DarknessRises.Game.Routes;

namespace DarknessRises.Game.Waves
{
    /// <summary>
    /// Runs each Station's Waves when the character reaches it, to the Station rhythm: ready, fight, clear, quiet, go.
    /// The Station is cleared at the end of it and the character walks on. A Station without authored Waves is cleared
    /// at once.
    /// </summary>
    public sealed class RouteWaves<TEnemyType> : IDisposable
    {
        private readonly RouteProgress progress;
        private readonly WaveSchedule<TEnemyType>[] schedules;
        private readonly StationFight<TEnemyType> fight;

        /// <param name="progress">The character's walk. Its Stations are matched to <paramref name="stationWaves"/> by index.</param>
        /// <param name="stationWaves">Each Station's Waves, by Station index.</param>
        /// <param name="spawner">Brings the Enemies in.</param>
        /// <param name="rhythm">
        /// The beats each stop plays. None: no beats, the Waves start on arrival and the character leaves with the last death.
        /// </param>
        public RouteWaves(
            RouteProgress progress,
            IReadOnlyList<IReadOnlyList<WavePlan<TEnemyType>>> stationWaves,
            IEnemySpawner<TEnemyType> spawner,
            StationRhythm rhythm = null)
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

            fight = new StationFight<TEnemyType>(rhythm ?? new StationRhythm(default));
            progress.StationReached += OnStationReached;
        }

        /// <summary>The Waves of the Station the character is at, or null while it walks.</summary>
        public WaveSchedule<TEnemyType> Current => fight.Waves;

        public void Tick(float deltaSeconds)
        {
            fight.Tick(deltaSeconds);
        }

        public void Dispose()
        {
            progress.StationReached -= OnStationReached;
            fight.Abandon();
        }

        private void OnStationReached(Station station)
        {
            if (station.Index >= schedules.Length)
            {
                station.Clear();
                return;
            }

            fight.Begin(station, schedules[station.Index]);
        }
    }
}
