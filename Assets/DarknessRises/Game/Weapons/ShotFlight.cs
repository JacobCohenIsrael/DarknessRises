using UnityEngine;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// Where a Shot is along its straight flight, and when it has used up its range.
    /// </summary>
    public struct ShotFlight
    {
        private readonly float speed;
        private readonly float range;

        public ShotFlight(Vector3 origin, Vector3 direction, float speed, float range)
        {
            Position = origin;
            Direction = direction.normalized;
            this.speed = speed;
            this.range = range;
            DistanceTravelled = 0f;
        }

        public Vector3 Position { get; private set; }
        public Vector3 Direction { get; }
        public float DistanceTravelled { get; private set; }
        public bool IsSpent => DistanceTravelled >= range;

        /// <summary>How far the Shot would move in <paramref name="deltaSeconds"/>, never past the end of its range.</summary>
        public float StepLength(float deltaSeconds)
        {
            return Mathf.Min(speed * deltaSeconds, Mathf.Max(0f, range - DistanceTravelled));
        }

        /// <summary>Moves the Shot <paramref name="distance"/> metres forward.</summary>
        public void Advance(float distance)
        {
            Position += Direction * distance;
            DistanceTravelled += distance;
        }

        /// <summary>Ends the flight early, for example when the Shot hits something.</summary>
        public void Stop(Vector3 at)
        {
            Position = at;
            DistanceTravelled = range;
        }
    }
}
