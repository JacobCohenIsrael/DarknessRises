using System;
using UnityEngine;

namespace DarknessRises.Game.Grenades
{
    /// <summary>
    /// How a Grenade's blast pushes regular Enemies outwards from its centre: hardest at the centre, falling off in a
    /// straight line to the edge of the blast radius. Before the Enemy type's multiplier and cap. Bosses ignore it.
    /// </summary>
    [Serializable]
    public sealed class GrenadeKnockbackSettings
    {
        [SerializeField, Min(0f), Tooltip("Metres the blast pushes an Enemy standing at its centre, before the Enemy type's multiplier and cap.")]
        private float centreMetres = 0.8f;

        [SerializeField, Min(0f), Tooltip("Metres the blast pushes an Enemy at the edge of the blast radius, before the Enemy type's multiplier and cap.")]
        private float edgeMetres = 0.2f;

        [SerializeField, Min(0.01f), Tooltip("Seconds of Enemy time the push takes. It starts fast and eases out.")]
        private float seconds = 0.25f;

        public float CentreMetres => centreMetres;
        public float EdgeMetres => edgeMetres;
        public float Seconds => seconds;

        /// <summary>Metres the blast pushes an Enemy <paramref name="distance"/> metres from its centre.</summary>
        public float MetresAt(float distance, float blastRadius)
        {
            return GrenadeKnockback.MetresAt(distance, blastRadius, centreMetres, edgeMetres);
        }
    }
}
