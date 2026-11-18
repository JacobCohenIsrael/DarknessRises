using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarknessRises.Game.Routes
{
    /// <summary>
    /// Where the character is on its Route. It walks on its own at the walking speed, stops at each Station
    /// until that Station is cleared, and stops for good at the end of the Route (ADR 0004).
    /// Time comes in as an explicit delta; nothing here reads Unity time.
    /// </summary>
    public sealed class RouteProgress
    {
        private readonly Station[] stations;
        private readonly float walkingSpeed;
        private readonly Action<Station> onStationCleared;
        private int nextStation;

        public RouteProgress(RoutePath path, IReadOnlyList<Station> stations, float walkingSpeed)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));

            if (stations == null)
            {
                throw new ArgumentNullException(nameof(stations));
            }

            if (walkingSpeed < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(walkingSpeed), walkingSpeed, "Walking speed can't be negative.");
            }

            this.stations = new Station[stations.Count];
            for (var i = 0; i < stations.Count; i++)
            {
                var station = stations[i] ?? throw new ArgumentException("A Route's Stations can't be null.", nameof(stations));
                if (station.Distance > path.Length)
                {
                    throw new ArgumentException($"Station {station.Index} sits past the end of the Route.", nameof(stations));
                }

                if (i > 0 && station.Distance < this.stations[i - 1].Distance)
                {
                    throw new ArgumentException("Stations must be listed in the order the Route reaches them.", nameof(stations));
                }

                this.stations[i] = station;
            }

            this.walkingSpeed = walkingSpeed;
            onStationCleared = OnStationCleared;
        }

        /// <summary>Raised when the character arrives at a Station and stops.</summary>
        public event Action<Station> StationReached;

        /// <summary>Raised when the Station the character is stopped at is cleared, so it walks on.</summary>
        public event Action<Station> StationLeft;

        /// <summary>Raised once, when the character reaches the end of the Route.</summary>
        public event Action Finished;

        public RoutePath Path { get; }

        /// <summary>The Route's Stations, in the order the character reaches them.</summary>
        public IReadOnlyList<Station> Stations => stations;

        /// <summary>Metres walked from the start of the Route.</summary>
        public float Distance { get; private set; }

        public Vector3 Position => Path.PositionAt(Distance);

        /// <summary>Unit direction the Route heads in from where the character is.</summary>
        public Vector3 Direction => Path.DirectionAt(Distance);

        /// <summary>The Station the character is stopped at, or null while it walks.</summary>
        public Station CurrentStation { get; private set; }

        public bool IsAtStation => CurrentStation != null;

        public bool IsFinished { get; private set; }

        /// <summary>
        /// Walks the character on. It stops exactly on a Station it reaches; the rest of that step is not carried over.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (IsFinished || IsAtStation)
            {
                return;
            }

            var reach = Distance + walkingSpeed * deltaSeconds;

            if (nextStation < stations.Length && reach >= stations[nextStation].Distance)
            {
                var station = stations[nextStation];
                nextStation++;
                Distance = station.Distance;
                Arrive(station);
                return;
            }

            Distance = Mathf.Min(reach, Path.Length);
            if (Distance >= Path.Length && nextStation == stations.Length)
            {
                IsFinished = true;
                Finished?.Invoke();
            }
        }

        /// <summary>
        /// Debug: puts the character straight on <paramref name="station"/>, past every Station before it, as if it had
        /// just walked there. It leaves the Station it was stopped at, if any. Only a Station still ahead counts.
        /// </summary>
        /// <returns>Whether the character moved.</returns>
        public bool SkipTo(Station station)
        {
            var index = Array.IndexOf(stations, station);
            if (index < 0 || index < nextStation - (IsAtStation ? 1 : 0) || station == CurrentStation || IsFinished)
            {
                return false;
            }

            if (IsAtStation)
            {
                Leave();
            }

            nextStation = index + 1;
            Distance = station.Distance;
            Arrive(station);
            return true;
        }

        private void Arrive(Station station)
        {
            CurrentStation = station;
            station.Cleared += onStationCleared;
            StationReached?.Invoke(station);

            // A Station cleared before the character got there (or by a StationReached listener) lets it walk straight on.
            if (CurrentStation == station && station.IsCleared)
            {
                Leave();
            }
        }

        private void OnStationCleared(Station station)
        {
            if (station == CurrentStation)
            {
                Leave();
            }
        }

        private void Leave()
        {
            var station = CurrentStation;
            station.Cleared -= onStationCleared;
            CurrentStation = null;
            StationLeft?.Invoke(station);
        }
    }
}
