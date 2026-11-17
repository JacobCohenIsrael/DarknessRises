using UnityEngine;

namespace DarknessRises.Game.Grenades
{
    /// <summary>
    /// Where a thrown Grenade is along its arc. It leaves the hand and comes down exactly on its target, travelling
    /// at a steady speed across the floor while gravity pulls it into an arc, so far throws fly longer and higher.
    /// </summary>
    public struct GrenadeFlight
    {
        private readonly Vector3 origin;
        private readonly Vector3 launchVelocity;
        private readonly float gravity;

        /// <param name="origin">Where the Grenade leaves the hand.</param>
        /// <param name="target">Where it comes down.</param>
        /// <param name="speed">Metres per second across the floor.</param>
        /// <param name="gravity">Metres per second squared pulling it down.</param>
        /// <param name="minSeconds">The shortest flight, so a throw at the character's feet still arcs.</param>
        public GrenadeFlight(Vector3 origin, Vector3 target, float speed, float gravity, float minSeconds)
        {
            var flat = target - origin;
            flat.y = 0f;

            this.origin = origin;
            this.gravity = gravity;
            Target = target;
            FlightSeconds = Mathf.Max(minSeconds, flat.magnitude / speed);
            SecondsFlown = 0f;
            Position = origin;

            // Solves origin + v·T − ½·g·T²·up = target for v.
            launchVelocity = (target - origin) / FlightSeconds + Vector3.up * (0.5f * gravity * FlightSeconds);
        }

        public Vector3 Target { get; }
        public Vector3 Position { get; private set; }
        public float FlightSeconds { get; }
        public float SecondsFlown { get; private set; }
        public bool HasLanded => SecondsFlown >= FlightSeconds;

        /// <summary>
        /// The point the Grenade is thrown at: <paramref name="aimPoint"/>, pulled in to at most <paramref name="maxRange"/>
        /// metres across the floor from <paramref name="from"/>.
        /// </summary>
        public static Vector3 ClampToRange(Vector3 from, Vector3 aimPoint, float maxRange)
        {
            var flat = aimPoint - from;
            flat.y = 0f;
            var distance = flat.magnitude;
            if (distance <= maxRange)
            {
                return aimPoint;
            }

            var clamped = from + flat * (maxRange / distance);
            clamped.y = aimPoint.y;
            return clamped;
        }

        /// <summary>Where the Grenade is <paramref name="seconds"/> after it left the hand, never past its landing.</summary>
        public Vector3 PositionAt(float seconds)
        {
            if (seconds >= FlightSeconds)
            {
                return Target;
            }

            return origin + launchVelocity * seconds + Vector3.down * (0.5f * gravity * seconds * seconds);
        }

        /// <summary>Moves the Grenade <paramref name="deltaSeconds"/> along its arc.</summary>
        public void Advance(float deltaSeconds)
        {
            SecondsFlown = Mathf.Min(FlightSeconds, SecondsFlown + deltaSeconds);
            Position = PositionAt(SecondsFlown);
        }

        /// <summary>Ends the flight early at <paramref name="point"/>, for example when the Grenade hits an Obstacle.</summary>
        public void Stop(Vector3 point)
        {
            Position = point;
            SecondsFlown = FlightSeconds;
        }
    }
}
