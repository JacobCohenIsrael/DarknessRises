using System;
using UnityEngine;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// How far one of a Weapon's hits shoves a regular Enemy along the Shot's direction, and how quickly. Before the
    /// Enemy type's multiplier and cap. Bosses ignore it.
    /// </summary>
    [Serializable]
    public sealed class KnockbackSettings
    {
        [SerializeField, Min(0f), Tooltip("Metres one hit pushes a regular Enemy along the Shot's direction, before the Enemy type's multiplier. 0 pushes nothing.")]
        private float metres = 0.35f;

        [SerializeField, Min(0.01f), Tooltip("Seconds of Enemy time the push takes. It starts fast and eases out.")]
        private float seconds = 0.12f;

        public float Metres => metres;
        public float Seconds => seconds;
    }
}
