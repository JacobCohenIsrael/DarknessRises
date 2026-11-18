using System;
using UnityEngine;

namespace DarknessRises.Game.Grenades
{
    /// <summary>The Grenade blast's Knockback falloff. Plain C# (ADR 0003).</summary>
    public static class GrenadeKnockback
    {
        /// <summary>
        /// Metres the blast pushes an Enemy <paramref name="distance"/> metres from its centre: <paramref name="centreMetres"/>
        /// at the centre, falling off in a straight line to <paramref name="edgeMetres"/> at <paramref name="blastRadius"/>,
        /// and nothing beyond it.
        /// </summary>
        public static float MetresAt(float distance, float blastRadius, float centreMetres, float edgeMetres)
        {
            if (blastRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(blastRadius), blastRadius, "The blast radius must be positive.");
            }

            if (centreMetres < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(centreMetres), centreMetres, "Knockback can't pull.");
            }

            if (edgeMetres < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(edgeMetres), edgeMetres, "Knockback can't pull.");
            }

            distance = Mathf.Abs(distance);
            return distance > blastRadius ? 0f : Mathf.Lerp(centreMetres, edgeMetres, distance / blastRadius);
        }
    }
}
