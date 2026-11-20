using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarknessRises.Game.Routes
{
    /// <summary>
    /// Where the character is on its Route. It walks on its own at the walking speed, stops at each Station
    /// until that Station is cleared, and stops for good at the end of the Route (ADR 0004). Its pace eases into each
    /// Station and out of it again, as its <see cref="WalkEasing"/> says.
    /// Time comes in as an explicit delta; nothing here reads Unity time.
    /// </summary>
    public sealed class RouteProgress
    {
        private readonly Station[] stations;
        private readonly float walkingSpeed;
        private readonly WalkEasing easing;
        private readonly Action<Station> onStationCleared;
        private int nextStation;

        // The straight walk back onto the Route after something else moved the character off it (Rejoin).
        private Vector3 leadInFrom;
        private Vector3 leadInTo;
        private float leadInLength;
        private float leadInWalked;
        private float joinDistance;

        // Counted from the last Station the character left. It walks in at full pace at the start of the Route.
        private float secondsSinceSetOff = float.PositiveInfinity;

        public RouteProgress(RoutePath path, IReadOnlyList<Station> stations, float walkingSpeed, WalkEasing easing = default)
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
            this.easing = easing;
            onStationCleared = OnStationCleared;
        }

        /// <summary>Raised when the character arrives at a Station and stops.</summary>
        public event Action<Station> StationReached;

        /// <summary>Raised when the Station the character is stopped at is cleared, so it walks on.</summary>
        public event Action<Station> StationLeft;

        /// <summary>Raised once, when the character reaches the end of the Route.</summary>
        public event Action Finished;

        /// <summary>Raised when <see cref="Rejoin"/> sets the character's way back onto the Route.</summary>
        public event Action Rejoined;

        public RoutePath Path { get; }

        /// <summary>The Route's Stations, in the order the character reaches them.</summary>
        public IReadOnlyList<Station> Stations => stations;

        /// <summary>
        /// Metres walked from the start of the Route. While the character walks back onto it (<see cref="IsRejoining"/>),
        /// this stays where it left the Route until it joins it again.
        /// </summary>
        public float Distance { get; private set; }

        /// <summary>Where the character is: on the Route, or on its straight way back onto it.</summary>
        public Vector3 Position => IsRejoining
            ? Vector3.Lerp(leadInFrom, leadInTo, leadInWalked / leadInLength)
            : Path.PositionAt(Distance);

        /// <summary>Unit direction the character heads in: the Route's from where it is, or its way back onto the Route.</summary>
        public Vector3 Direction => IsRejoining ? (leadInTo - leadInFrom) / leadInLength : Path.DirectionAt(Distance);

        /// <summary>Whether the character is walking back onto the Route, off it, after <see cref="Rejoin"/>.</summary>
        public bool IsRejoining => leadInWalked < leadInLength;

        /// <summary>Metres along the Route of the next Station the character will stop at, or of the Route's end.</summary>
        public float NextStopDistance => nextStation < stations.Length ? stations[nextStation].Distance : Path.Length;

        /// <summary>The Station the character is stopped at, or null while it walks.</summary>
        public Station CurrentStation { get; private set; }

        public bool IsAtStation => CurrentStation != null;

        public bool IsFinished { get; private set; }

        /// <summary>
        /// The share of the walking speed the character moved at in the last tick: 1 at full pace, less while it eases into
        /// or out of a Station, 0 while it stands at one or at the end of the Route.
        /// </summary>
        public float SpeedFraction { get; private set; }

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
                SpeedFraction = 0f;
                return;
            }

            var rejoining = IsRejoining;
            var metresToStation = nextStation < stations.Length
                ? stations[nextStation].Distance - (rejoining ? joinDistance - (leadInLength - leadInWalked) : Distance)
                : float.PositiveInfinity;
            SpeedFraction = easing.SpeedFraction(metresToStation, secondsSinceSetOff);
            secondsSinceSetOff += deltaSeconds;
            var step = walkingSpeed * SpeedFraction * deltaSeconds;

            // Back onto the Route first; what's left of the step walks on along it.
            if (rejoining)
            {
                var leadInLeft = leadInLength - leadInWalked;
                if (step < leadInLeft)
                {
                    leadInWalked += step;
                    return;
                }

                step -= leadInLeft;
                EndLeadIn(joinDistance);
            }

            var reach = Distance + step;

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
                SpeedFraction = 0f;
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

            EndLeadIn(Distance);
            nextStation = index + 1;
            Distance = station.Distance;
            Arrive(station);
            return true;
        }

        /// <summary>
        /// The character stands at <paramref name="from"/>, where something else (a Boss fight) moved it: from now on it
        /// walks in a straight line from there to the Route, <paramref name="joinAt"/> metres along it, and on along the
        /// Route, at the walking speed and with the usual easing. Where it stands is where it is at once, so nothing
        /// that places it jumps. <paramref name="joinAt"/> lies between <see cref="Distance"/> and
        /// <see cref="NextStopDistance"/> (<see cref="RouteRejoin"/> picks it). A character at a Station only walks off
        /// once the Station is cleared.
        /// </summary>
        public void Rejoin(Vector3 from, float joinAt)
        {
            if (joinAt < Distance || joinAt > NextStopDistance)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(joinAt), joinAt, "The character rejoins the Route between where it left it and its next stop.");
            }

            if (IsFinished)
            {
                return;
            }

            leadInFrom = from;
            leadInTo = Path.PositionAt(joinAt);
            leadInLength = Vector3.Distance(from, leadInTo);
            leadInWalked = 0f;
            joinDistance = joinAt;
            if (!IsRejoining)
            {
                EndLeadIn(joinAt);
            }

            Rejoined?.Invoke();
        }

        /// <summary>
        /// The point <paramref name="metres"/> further along the character's way: along its way back onto the Route
        /// while it rejoins, then along the Route, clamped to the Route's end. The camera aims here.
        /// </summary>
        public Vector3 PositionAhead(float metres)
        {
            metres = Mathf.Max(metres, 0f);
            if (!IsRejoining)
            {
                return Path.PositionAt(Distance + metres);
            }

            var walked = leadInWalked + metres;
            return walked < leadInLength
                ? Vector3.Lerp(leadInFrom, leadInTo, walked / leadInLength)
                : Path.PositionAt(joinDistance + walked - leadInLength);
        }

        private void EndLeadIn(float distance)
        {
            leadInLength = 0f;
            leadInWalked = 0f;
            Distance = distance;
        }

        private void Arrive(Station station)
        {
            CurrentStation = station;
            SpeedFraction = 0f;
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
            secondsSinceSetOff = 0f;
            StationLeft?.Invoke(station);
        }
    }
}
