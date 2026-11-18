using System;
using UnityEngine;

namespace DarknessRises.Game.Continues
{
    /// <summary>
    /// How Continue's burst of light pushes Enemies back: every Enemy within the radius slides straight away from the
    /// character, at the push-back force, until it's out at the radius. Enemies further out aren't touched.
    /// Distances are measured across the floor; height is ignored. Plain C# (ADR 0003).
    /// </summary>
    public sealed class ContinuePushBack
    {
        // Closer than this, an Enemy is on top of the character and has no direction of its own to be pushed in.
        private const float OnTopDistance = 0.0001f;

        private readonly float radius;
        private readonly float force;

        public ContinuePushBack(float radius, float force)
        {
            if (radius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(radius), radius, "The push-back needs a radius.");
            }

            if (force <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(force), force, "The push-back needs a force.");
            }

            this.radius = radius;
            this.force = force;
        }

        /// <summary>
        /// The push-back for an Enemy at <paramref name="enemy"/> when the burst goes off at <paramref name="centre"/>.
        /// An Enemy right on top of the centre is pushed along <paramref name="fallbackDirection"/> (e.g. where the
        /// character faces).
        /// </summary>
        public PushBack For(Vector3 centre, Vector3 enemy, Vector3 fallbackDirection)
        {
            var offset = enemy - centre;
            offset.y = 0f;
            var distance = offset.magnitude;
            if (distance >= radius)
            {
                return PushBack.None;
            }

            Vector3 direction;
            if (distance > OnTopDistance)
            {
                direction = offset / distance;
            }
            else
            {
                fallbackDirection.y = 0f;
                direction = fallbackDirection.sqrMagnitude > 0f ? fallbackDirection.normalized : Vector3.forward;
            }

            return new PushBack(direction * force, (radius - distance) / force);
        }
    }

    /// <summary>One Enemy's push-back: it slides at <see cref="Velocity"/> for <see cref="Seconds"/>.</summary>
    public readonly struct PushBack
    {
        public static readonly PushBack None = default;

        public PushBack(Vector3 velocity, float seconds)
        {
            Velocity = velocity;
            Seconds = seconds;
        }

        /// <summary>Metres per second across the floor, away from the character.</summary>
        public Vector3 Velocity { get; }

        public float Seconds { get; }

        public bool Moves => Seconds > 0f;

        /// <summary>Where the Enemy ends up, relative to where it was.</summary>
        public Vector3 Displacement => Velocity * Seconds;
    }
}
