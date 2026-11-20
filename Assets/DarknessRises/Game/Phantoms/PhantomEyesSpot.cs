using System;
using DarknessRises.Game.Randomness;
using UnityEngine;

namespace DarknessRises.Game.Phantoms
{
    /// <summary>
    /// Where a pair of Phantom eyes may look out from: ahead of the character, within an angle of its facing, far out at
    /// the edge of the dark, beyond the Aura and the distance at which real eyes glint. The caller puts the spot on the
    /// floor and checks that it's dark and in view. Plain C# (ADR 0003).
    /// </summary>
    public readonly struct PhantomEyesSpot
    {
        public PhantomEyesSpot(float aheadDegrees, float minMetres, float maxMetres)
        {
            if (aheadDegrees < 0f || aheadDegrees > 180f)
            {
                throw new ArgumentOutOfRangeException(nameof(aheadDegrees), aheadDegrees, "From 0 to 180 degrees.");
            }

            if (minMetres < 0f || maxMetres < minMetres)
            {
                throw new ArgumentOutOfRangeException(nameof(maxMetres), maxMetres, "A range runs from a non-negative least to a most at least as big.");
            }

            AheadDegrees = aheadDegrees;
            MinMetres = minMetres;
            MaxMetres = maxMetres;
        }

        public float AheadDegrees { get; }
        public float MinMetres { get; }
        public float MaxMetres { get; }

        /// <summary>A spot for the eyes, seen from <paramref name="character"/> facing <paramref name="forward"/>, at its height.</summary>
        public Vector3 Pick(Vector3 character, Vector3 forward, IRandom random)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0f)
            {
                forward = Vector3.forward;
            }

            var turn = random.Range(-AheadDegrees, AheadDegrees);
            var direction = Quaternion.AngleAxis(turn, Vector3.up) * forward.normalized;
            return character + direction * random.Range(MinMetres, MaxMetres);
        }

        /// <summary>
        /// Whether <paramref name="spot"/> stays at least <paramref name="keepAwayMetres"/> across the floor from the
        /// character all the way along its next <paramref name="walkMetres"/> of walking along <paramref name="forward"/>,
        /// so the walk doesn't bring the character up to the eyes while they show.
        /// </summary>
        public static bool StaysAway(Vector3 spot, Vector3 character, Vector3 forward, float walkMetres, float keepAwayMetres)
        {
            forward.y = 0f;
            var offset = spot - character;
            offset.y = 0f;
            if (forward.sqrMagnitude <= 0f || walkMetres <= 0f)
            {
                return offset.magnitude >= keepAwayMetres;
            }

            // The nearest the walk comes to the spot: along the walked segment, the point nearest to it.
            forward.Normalize();
            var along = Mathf.Clamp(Vector3.Dot(offset, forward), 0f, walkMetres);
            return (offset - forward * along).magnitude >= keepAwayMetres;
        }
    }
}
