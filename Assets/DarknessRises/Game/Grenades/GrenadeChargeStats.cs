using System;

namespace DarknessRises.Game.Grenades
{
    /// <summary>
    /// The numbers <see cref="GrenadeCharges"/> needs, copied out of a <see cref="GrenadeDefinition"/>
    /// so the rule never holds on to authored data.
    /// </summary>
    public readonly struct GrenadeChargeStats
    {
        public GrenadeChargeStats(int maxCharges, float refillSeconds)
        {
            if (maxCharges < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCharges), maxCharges, "The character carries at least one Grenade.");
            }

            if (refillSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(refillSeconds), refillSeconds, "A charge takes positive time to refill.");
            }

            MaxCharges = maxCharges;
            RefillSeconds = refillSeconds;
        }

        public int MaxCharges { get; }
        public float RefillSeconds { get; }
    }
}
