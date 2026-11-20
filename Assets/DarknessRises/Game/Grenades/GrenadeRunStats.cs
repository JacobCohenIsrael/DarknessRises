using System;

namespace DarknessRises.Game.Grenades
{
    /// <summary>
    /// The Grenade's numbers for one Run: its authored ones with the player's Upgrades applied. The charges' rule and
    /// the explosion read these, never the definition's own numbers.
    /// </summary>
    public readonly struct GrenadeRunStats
    {
        public GrenadeRunStats(GrenadeChargeStats charges, float blastRadius)
        {
            if (blastRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(blastRadius), blastRadius, "An explosion reaches some way.");
            }

            Charges = charges;
            BlastRadius = blastRadius;
        }

        /// <summary>How many Grenades the character carries and how long a missing one takes to refill.</summary>
        public GrenadeChargeStats Charges { get; }

        /// <summary>Metres from the explosion within which Enemies are damaged.</summary>
        public float BlastRadius { get; }
    }
}
