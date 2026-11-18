using System;
using System.Collections.Generic;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Waves;

namespace DarknessRises.Game.Levels
{
    /// <summary>
    /// Everything the character fights on a Level's Route: each Station's Waves when it gets there, and Stragglers
    /// while it walks between Stations. The Route's last Station is the Level's end: it is left to the owner,
    /// who clears it once the end beat has played.
    /// </summary>
    public sealed class LevelFights<TEnemyType> : IDisposable
    {
        private readonly RouteProgress progress;
        private readonly WaveSchedule<TEnemyType>[] schedules;
        private readonly StragglerSchedule<TEnemyType> stragglers;
        private readonly int exitStation;
        private WaveSchedule<TEnemyType> current;
        private Station currentStation;

        /// <param name="progress">The character's walk through the Level.</param>
        /// <param name="stationWaves">
        /// The Waves of every Station but the last, by Station index. A Station without Waves is cleared at once.
        /// </param>
        /// <param name="waveSpawner">Brings the Waves' Enemies in.</param>
        /// <param name="stragglers">Sends Stragglers while the character walks between Stations.</param>
        public LevelFights(
            RouteProgress progress,
            IReadOnlyList<IReadOnlyList<WavePlan<TEnemyType>>> stationWaves,
            IEnemySpawner<TEnemyType> waveSpawner,
            StragglerSchedule<TEnemyType> stragglers)
        {
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
            this.stragglers = stragglers ?? throw new ArgumentNullException(nameof(stragglers));

            if (stationWaves == null)
            {
                throw new ArgumentNullException(nameof(stationWaves));
            }

            if (waveSpawner == null)
            {
                throw new ArgumentNullException(nameof(waveSpawner));
            }

            // Built up front so reaching a Station allocates nothing.
            exitStation = stationWaves.Count;
            schedules = new WaveSchedule<TEnemyType>[stationWaves.Count];
            for (var i = 0; i < stationWaves.Count; i++)
            {
                schedules[i] = new WaveSchedule<TEnemyType>(stationWaves[i], waveSpawner);
            }

            progress.StationReached += OnStationReached;
        }

        /// <summary>Raised when the character reaches the Route's last Station, the end of the Level. The owner clears it.</summary>
        public event Action<Station> ExitReached;

        /// <summary>Raised when the character reaches a Station with Waves to fight. A Station without Waves raises nothing.</summary>
        public event Action<Station> FightStarted;

        /// <summary>Raised when a Station's last Wave is dead and the character walks on.</summary>
        public event Action<Station> FightCleared;

        /// <summary>The fight at the Station the character is at, or null while it walks.</summary>
        public WaveSchedule<TEnemyType> Current => current;

        /// <summary>Whether Stragglers may come. Off during the Level's start and end beats.</summary>
        public bool StragglersAllowed { get; set; }

        public StragglerSchedule<TEnemyType> Stragglers => stragglers;

        /// <summary>Enemies of this Level alive right now: the current Station's and the Stragglers.</summary>
        public int AliveCount => (current != null ? current.AliveCount : 0) + stragglers.AliveCount;

        public void Tick(float deltaSeconds)
        {
            if (current != null)
            {
                current.Tick(deltaSeconds);
                if (current.IsDone)
                {
                    current = null;
                    FightCleared?.Invoke(currentStation);
                    currentStation = null;
                }
            }

            var walking = StragglersAllowed && !progress.IsAtStation && !progress.IsFinished;
            stragglers.Tick(deltaSeconds, walking);
        }

        public void Dispose()
        {
            progress.StationReached -= OnStationReached;
            current = null;
        }

        private void OnStationReached(Station station)
        {
            if (station.Index >= exitStation)
            {
                ExitReached?.Invoke(station);
                return;
            }

            var schedule = schedules[station.Index];
            schedule.Begin(station);
            if (schedule.IsDone)
            {
                return;
            }

            current = schedule;
            currentStation = station;
            FightStarted?.Invoke(station);
        }
    }
}
