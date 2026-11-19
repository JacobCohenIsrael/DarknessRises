using System;
using System.Collections.Generic;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Waves;

namespace DarknessRises.Game.Levels
{
    /// <summary>
    /// Everything the character fights on a Level's Route: each Station's Waves when it gets there, run to the Station
    /// rhythm (ready, fight, clear, quiet, go), and Stragglers while it walks between Stations.
    /// The Route's last Station is the Level's end: it is left to the owner,
    /// who clears it once the end beat has played. In a Level with a Boss, the Station before it is the Boss's:
    /// it too is left to the owner, who clears it once the Boss is beaten.
    /// </summary>
    public sealed class LevelFights<TEnemyType> : IDisposable
    {
        private readonly RouteProgress progress;
        private readonly WaveSchedule<TEnemyType>[] schedules;
        private readonly StragglerSchedule<TEnemyType> stragglers;
        private readonly int exitStation;
        private readonly int bossStation;
        private readonly StationFight<TEnemyType> fight;

        /// <param name="progress">The character's walk through the Level.</param>
        /// <param name="stationWaves">
        /// The Waves of every Station but the last, by Station index. A Station without Waves is cleared at once.
        /// </param>
        /// <param name="waveSpawner">Brings the Waves' Enemies in.</param>
        /// <param name="stragglers">Sends Stragglers while the character walks between Stations.</param>
        /// <param name="hasBoss">
        /// Whether the Level ends in a Boss fight, at the Station right after the last one in <paramref name="stationWaves"/>.
        /// </param>
        /// <param name="rhythm">
        /// The beats each stop at a Station with Waves plays. None: no beats, the Waves start on arrival and the character
        /// leaves with the last death.
        /// </param>
        public LevelFights(
            RouteProgress progress,
            IReadOnlyList<IReadOnlyList<WavePlan<TEnemyType>>> stationWaves,
            IEnemySpawner<TEnemyType> waveSpawner,
            StragglerSchedule<TEnemyType> stragglers,
            bool hasBoss = false,
            StationRhythm rhythm = null)
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
            bossStation = hasBoss ? stationWaves.Count : -1;
            exitStation = stationWaves.Count + (hasBoss ? 1 : 0);
            schedules = new WaveSchedule<TEnemyType>[stationWaves.Count];
            for (var i = 0; i < stationWaves.Count; i++)
            {
                schedules[i] = new WaveSchedule<TEnemyType>(stationWaves[i], waveSpawner);
            }

            fight = new StationFight<TEnemyType>(rhythm ?? new StationRhythm(default));
            progress.StationReached += OnStationReached;
        }

        /// <summary>Raised when the character reaches the Route's last Station, the end of the Level. The owner clears it.</summary>
        public event Action<Station> ExitReached;

        /// <summary>Raised when the character reaches the Boss's Station. The owner runs the Boss fight and clears it.</summary>
        public event Action<Station> BossReached;

        /// <summary>
        /// Raised when the character stops at a Station with Waves to fight, as its Ready beat starts.
        /// A Station without Waves raises nothing.
        /// </summary>
        public event Action<Station> FightStarted;

        /// <summary>
        /// Raised when the stop is over: the Station's last Wave is dead, the beats after it have played, and the
        /// character sets off.
        /// </summary>
        public event Action<Station> FightCleared;

        /// <summary>The Waves of the Station the character is stopped at, or null while it walks.</summary>
        public WaveSchedule<TEnemyType> Current => fight.Waves;

        /// <summary>Whether Stragglers may come. Off during the Level's start and end beats.</summary>
        public bool StragglersAllowed { get; set; }

        public StragglerSchedule<TEnemyType> Stragglers => stragglers;

        /// <summary>Enemies of this Level alive right now: the current Station's and the Stragglers.</summary>
        public int AliveCount => (fight.Waves != null ? fight.Waves.AliveCount : 0) + stragglers.AliveCount;

        public void Tick(float deltaSeconds)
        {
            var station = fight.Station;
            if (fight.Tick(deltaSeconds))
            {
                FightCleared?.Invoke(station);
            }

            var walking = StragglersAllowed && !progress.IsAtStation && !progress.IsFinished;
            stragglers.Tick(deltaSeconds, walking);
        }

        /// <summary>
        /// Debug: gives up on the current Station's fight as if it were cleared, leaving its Enemies to whoever holds them.
        /// </summary>
        public void AbandonCurrent()
        {
            var station = fight.Abandon();
            if (station != null)
            {
                FightCleared?.Invoke(station);
            }
        }

        public void Dispose()
        {
            progress.StationReached -= OnStationReached;
            fight.Abandon();
        }

        private void OnStationReached(Station station)
        {
            if (station.Index >= exitStation)
            {
                ExitReached?.Invoke(station);
                return;
            }

            if (station.Index == bossStation)
            {
                BossReached?.Invoke(station);
                return;
            }

            if (fight.Begin(station, schedules[station.Index]))
            {
                FightStarted?.Invoke(station);
            }
        }
    }
}
