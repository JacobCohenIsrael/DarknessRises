using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarknessRises.Game.Bosses
{
    /// <summary>
    /// Relocate (ADR 0006): the character's quick dash between a Boss Room's marked Positions, picked by direction.
    /// It's a discrete choice, never steering, and only works while the Boss fight enables it.
    /// Plain C# (ADR 0003): time comes in as an explicit delta, directions as method calls.
    /// </summary>
    /// <remarks>
    /// A direction is on the floor plane as seen from the fixed camera: x is east (screen right), y is north
    /// (screen up). Keyboard keys, and later touch flicks, both come in as such a direction.
    /// </remarks>
    public sealed class Relocation
    {
        private readonly Vector3[] positions;
        private readonly RelocateSettings settings;
        private float dashElapsedSeconds;

        /// <param name="positions">The Boss Room's Positions, in world space.</param>
        /// <param name="startPosition">The Position the character stands on when the fight starts.</param>
        public Relocation(IReadOnlyList<Vector3> positions, int startPosition, RelocateSettings settings)
        {
            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            if (positions.Count == 0)
            {
                throw new ArgumentException("A Boss Room needs at least one Position.", nameof(positions));
            }

            if (startPosition < 0 || startPosition >= positions.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(startPosition), startPosition, "No such Position.");
            }

            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.positions = new Vector3[positions.Count];
            for (var i = 0; i < positions.Count; i++)
            {
                this.positions[i] = positions[i];
            }

            Position = startPosition;
            Destination = startPosition;
        }

        /// <summary>Raised when a dash starts: from which Position, to which.</summary>
        public event Action<int, int> DashStarted;

        /// <summary>Raised when the character arrives on a Position.</summary>
        public event Action<int> Arrived;

        public IReadOnlyList<Vector3> Positions => positions;

        /// <summary>Whether the player may Relocate right now. The Boss fight switches it on and off.</summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// The Position the character stands on. While it dashes, that's still the one it left: it hasn't made it
        /// out from under a stomp until it arrives.
        /// </summary>
        public int Position { get; private set; }

        /// <summary>Where the character is headed: the Position it stands on, or the one it dashes to.</summary>
        public int Destination { get; private set; }

        public bool IsDashing => Destination != Position;

        /// <summary>Where the character is in the Room, along the dash if it's dashing.</summary>
        public Vector3 Point
        {
            get
            {
                if (!IsDashing)
                {
                    return positions[Position];
                }

                var t = Mathf.Clamp01(dashElapsedSeconds / settings.DashSeconds);
                return Vector3.Lerp(positions[Position], positions[Destination], Mathf.SmoothStep(0f, 1f, t));
            }
        }

        /// <summary>
        /// The nearest Position to <paramref name="from"/> that lies within <paramref name="maxAngleDegrees"/> of
        /// <paramref name="direction"/>, or -1 when none does. Ties go to the one closer to the direction.
        /// </summary>
        public static int PickInDirection(IReadOnlyList<Vector3> positions, int from, Vector2 direction, float maxAngleDegrees)
        {
            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            if (direction.sqrMagnitude <= 0f)
            {
                return -1;
            }

            var heading = direction.normalized;
            var minCos = Mathf.Cos(maxAngleDegrees * Mathf.Deg2Rad);
            var origin = positions[from];
            var best = -1;
            var bestDistance = float.PositiveInfinity;
            var bestCos = -1f;
            for (var i = 0; i < positions.Count; i++)
            {
                if (i == from)
                {
                    continue;
                }

                var offset = new Vector2(positions[i].x - origin.x, positions[i].z - origin.z);
                var distance = offset.magnitude;
                if (distance <= 0f)
                {
                    continue;
                }

                var cos = Vector2.Dot(offset / distance, heading);
                if (cos < minCos - 1e-5f)
                {
                    continue;
                }

                const float tieMetres = 0.01f;
                var nearer = distance < bestDistance - tieMetres;
                var tiedButStraighter = Mathf.Abs(distance - bestDistance) <= tieMetres && cos > bestCos;
                if (nearer || tiedButStraighter)
                {
                    best = i;
                    bestDistance = distance;
                    bestCos = cos;
                }
            }

            return best;
        }

        /// <summary>The Position nearest to <paramref name="position"/>, other than itself, or -1 when there is none.</summary>
        public int NearestOther(int position)
        {
            var best = -1;
            var bestSqr = float.PositiveInfinity;
            for (var i = 0; i < positions.Length; i++)
            {
                if (i == position)
                {
                    continue;
                }

                var sqr = (positions[i] - positions[position]).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    best = i;
                    bestSqr = sqr;
                }
            }

            return best;
        }

        /// <summary>
        /// Relocates to the nearest Position in <paramref name="direction"/>, if Relocate is enabled, the character
        /// isn't mid-dash and there is such a Position.
        /// </summary>
        /// <returns>Whether a dash started.</returns>
        public bool TryRelocate(Vector2 direction)
        {
            if (!IsEnabled || IsDashing)
            {
                return false;
            }

            var target = PickInDirection(positions, Position, direction, settings.MaxAngleDegrees);
            if (target < 0)
            {
                return false;
            }

            StartDash(target);
            return true;
        }

        /// <summary>
        /// Sends the character to <paramref name="position"/> whether or not Relocate is enabled: the fight's own
        /// moves, such as walking back onto the Route once the Boss is beaten. A dash under way is finished first.
        /// </summary>
        public void MoveTo(int position)
        {
            if (position < 0 || position >= positions.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(position), position, "No such Position.");
            }

            if (IsDashing)
            {
                Arrive();
            }

            if (position != Position)
            {
                StartDash(position);
            }
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (!IsDashing)
            {
                return;
            }

            dashElapsedSeconds += deltaSeconds;
            if (dashElapsedSeconds >= settings.DashSeconds)
            {
                Arrive();
            }
        }

        private void StartDash(int target)
        {
            var from = Position;
            Destination = target;
            dashElapsedSeconds = 0f;
            DashStarted?.Invoke(from, target);
        }

        private void Arrive()
        {
            Position = Destination;
            dashElapsedSeconds = 0f;
            Arrived?.Invoke(Position);
        }
    }
}
