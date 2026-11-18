using System;
using UnityEngine;

namespace DarknessRises.Game.Enemies
{
    /// <summary>
    /// An Enemy's Knockback: hits push it across the floor along their direction, starting fast and easing out. A new
    /// hit adds to what's left of the current push and starts the ease over, but the push left to travel never exceeds
    /// the cap and never starts faster than the speed limit. Plain C# (ADR 0003); time comes in as Enemy time, and the
    /// view moves the Enemy by what each <see cref="Step"/> returns. Separate from the Stagger: it never changes what
    /// the Enemy is doing. Pooled with its Enemy, so <see cref="Clear"/> starts it over.
    /// </summary>
    public sealed class EnemyKnockback
    {
        // The ease-out's speed at its start, as a multiple of its average speed: d/dt (1 - (1 - t)^2) at t = 0.
        private const float EaseStartSpeed = 2f;

        private readonly float multiplier;
        private readonly float maxMetres;
        private readonly float maxMetresPerSecond;
        private Vector3 push;
        private float duration;
        private float elapsed;

        public EnemyKnockback(float multiplier, float maxMetres, float maxMetresPerSecond)
        {
            this.multiplier = multiplier >= 0f
                ? multiplier
                : throw new ArgumentOutOfRangeException(nameof(multiplier), multiplier, "The multiplier can't be negative.");
            this.maxMetres = maxMetres >= 0f
                ? maxMetres
                : throw new ArgumentOutOfRangeException(nameof(maxMetres), maxMetres, "The cap can't be negative.");
            this.maxMetresPerSecond = maxMetresPerSecond > 0f
                ? maxMetresPerSecond
                : throw new ArgumentOutOfRangeException(nameof(maxMetresPerSecond), maxMetresPerSecond, "The speed limit must be positive.");
        }

        /// <summary>Whether a push is still under way.</summary>
        public bool IsMoving => elapsed < duration;

        /// <summary>How far the current push still has to go, flat on the floor.</summary>
        public Vector3 Remaining => IsMoving ? push * (1f - Ease(elapsed / duration)) : Vector3.zero;

        /// <summary>Seconds of Enemy time left in the current push.</summary>
        public float SecondsLeft => IsMoving ? duration - elapsed : 0f;

        /// <summary>How fast the current push moves right now, in metres per second of Enemy time.</summary>
        public float Speed => IsMoving ? EaseStartSpeed * push.magnitude * (1f - elapsed / duration) / duration : 0f;

        /// <summary>The ease-out: how much of a push is done at <paramref name="progress"/> (0 to 1) through its time.</summary>
        public static float Ease(float progress)
        {
            var left = 1f - Mathf.Clamp01(progress);
            return 1f - left * left;
        }

        /// <summary>
        /// Pushes along <paramref name="direction"/> (flattened) by <paramref name="metres"/> times the multiplier, over
        /// at least <paramref name="seconds"/>. Adds to what's left of the current push, within the cap and speed limit.
        /// </summary>
        public void Push(Vector3 direction, float metres, float seconds)
        {
            if (metres < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(metres), metres, "Knockback can't pull.");
            }

            if (seconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Knockback can't take negative time.");
            }

            direction.y = 0f;
            var distance = metres * multiplier;
            if (distance <= 0f || direction.sqrMagnitude <= 0f)
            {
                return;
            }

            var next = Vector3.ClampMagnitude(Remaining + direction.normalized * distance, maxMetres);
            var length = next.magnitude;
            if (length <= 0f)
            {
                Clear();
                return;
            }

            push = next;
            duration = Mathf.Max(Mathf.Max(seconds, SecondsLeft), EaseStartSpeed * length / maxMetresPerSecond);
            elapsed = 0f;
        }

        /// <summary>Moves the push on by <paramref name="deltaSeconds"/> of Enemy time.</summary>
        /// <returns>How far the Enemy moves in that time.</returns>
        public Vector3 Step(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (!IsMoving)
            {
                return Vector3.zero;
            }

            var before = Ease(elapsed / duration);
            elapsed = Mathf.Min(duration, elapsed + deltaSeconds);
            return push * (Ease(elapsed / duration) - before);
        }

        /// <summary>Drops any push under way, for a fresh spawn or a body coming to rest.</summary>
        public void Clear()
        {
            push = Vector3.zero;
            duration = 0f;
            elapsed = 0f;
        }
    }
}
