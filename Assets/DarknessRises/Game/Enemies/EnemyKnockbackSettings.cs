using System;
using UnityEngine;

namespace DarknessRises.Game.Enemies
{
    /// <summary>How an Enemy type takes Knockback: how heavy it is, and how far and fast repeated hits can push it.</summary>
    [Serializable]
    public sealed class EnemyKnockbackSettings
    {
        [SerializeField, Min(0f), Tooltip("Multiplies every hit's Knockback distance. Above 1 is lighter and flies further; 0 never moves.")]
        private float multiplier = 1f;

        [SerializeField, Min(0f), Tooltip("Metres of push the Enemy can have left to travel at once. Repeated hits add up to this and no further, so a burst can't launch it across the Room.")]
        private float maxMetres = 1f;

        [SerializeField, Min(0.1f), Tooltip("Fastest the Enemy is ever pushed, in metres per second of Enemy time. A push that would start faster is stretched out instead.")]
        private float maxMetresPerSecond = 8f;

        public float Multiplier => multiplier;
        public float MaxMetres => maxMetres;
        public float MaxMetresPerSecond => maxMetresPerSecond;
    }
}
