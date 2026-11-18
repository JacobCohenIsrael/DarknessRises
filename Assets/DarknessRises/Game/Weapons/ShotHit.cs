using System;
using UnityEngine;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// One hit by a Shot or a Grenade's blast: the damage, the direction it travels across the floor and the Knockback
    /// it carries. Targets that can't be pushed, such as the Colossus, only read the damage.
    /// </summary>
    public readonly struct ShotHit
    {
        public ShotHit(int damage, Vector3 direction, float knockbackMetres, float knockbackSeconds)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), damage, "Damage can't be negative.");
            }

            if (knockbackMetres < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(knockbackMetres), knockbackMetres, "Knockback can't pull.");
            }

            if (knockbackSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(knockbackSeconds), knockbackSeconds, "Knockback can't take negative time.");
            }

            Damage = damage;
            Direction = direction;
            KnockbackMetres = knockbackMetres;
            KnockbackSeconds = knockbackSeconds;
        }

        public int Damage { get; }

        /// <summary>Which way the hit travels. Not necessarily normalized or flat.</summary>
        public Vector3 Direction { get; }

        /// <summary>Metres the hit pushes a regular Enemy, before the Enemy type's multiplier and cap.</summary>
        public float KnockbackMetres { get; }

        /// <summary>Seconds of Enemy time the push takes.</summary>
        public float KnockbackSeconds { get; }
    }
}
