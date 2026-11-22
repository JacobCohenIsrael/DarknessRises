namespace DarknessRises.Game.Grenades
{
    /// <summary>
    /// How full each Grenade icon on the HUD is drawn: carried charges are full, the one refilling fills up with the
    /// refill, and the spent ones after it are empty, so they show only their dimmed outline. Plain C# (ADR 0003).
    /// </summary>
    public static class GrenadeChargeIcons
    {
        /// <summary>The fill of the icon at <paramref name="index"/> (0 first), from 0 (spent) to 1 (carried).</summary>
        public static float Fill(int index, int carried, float refillProgress)
        {
            if (index < carried)
            {
                return 1f;
            }

            if (index > carried)
            {
                return 0f;
            }

            return refillProgress < 0f ? 0f : refillProgress > 1f ? 1f : refillProgress;
        }

        /// <inheritdoc cref="Fill(int, int, float)"/>
        public static float Fill(int index, GrenadeCharges charges) => Fill(index, charges.Charges, charges.RefillProgress);
    }
}
