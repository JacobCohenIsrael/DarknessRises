using System;
using System.Collections.Generic;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// Which Primary weapons the character carries into a Run: every one the player has unlocked, in the Shop's order,
    /// so the one owned from the start (the Pistol) comes first and is the one equipped. A Primary that isn't unlocked
    /// isn't carried, so it can't be selected and none of its content is loaded. Plain C# (ADR 0003).
    /// </summary>
    public static class CarriedPrimaries
    {
        /// <param name="all">Every Primary there is, in the Shop's order.</param>
        /// <param name="isUnlocked">Whether the player owns a Primary.</param>
        /// <param name="slots">
        /// For each Primary in <paramref name="all"/>, where it is among the carried ones, or -1 when it isn't carried:
        /// what the "weapon 1, 2, 3" commands look up.
        /// </param>
        /// <returns>The carried Primaries, in switching order.</returns>
        public static T[] For<T>(IReadOnlyList<T> all, Func<T, bool> isUnlocked, out int[] slots) where T : class
        {
            if (all == null)
            {
                throw new ArgumentNullException(nameof(all));
            }

            if (isUnlocked == null)
            {
                throw new ArgumentNullException(nameof(isUnlocked));
            }

            slots = new int[all.Count];
            var carried = new List<T>(all.Count);
            for (var i = 0; i < all.Count; i++)
            {
                if (isUnlocked(all[i]))
                {
                    slots[i] = carried.Count;
                    carried.Add(all[i]);
                }
                else
                {
                    slots[i] = -1;
                }
            }

            if (carried.Count == 0)
            {
                throw new ArgumentException("The player owns no Primary weapon: one must be owned from the start.", nameof(isUnlocked));
            }

            return carried.ToArray();
        }
    }
}
