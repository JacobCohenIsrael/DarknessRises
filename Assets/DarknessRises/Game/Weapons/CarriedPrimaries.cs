using System;
using System.Collections.Generic;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// Which Primary weapons the character carries into a Run. A Run has one Primary, the one chosen in the Shop, so
    /// only that one is carried (and only its content loaded). Debug builds carry every Primary, with the chosen one
    /// equipped, so the debug switch can still try the others. Plain C# (ADR 0003).
    /// </summary>
    public static class CarriedPrimaries
    {
        /// <param name="all">Every Primary there is, in the Shop's order.</param>
        /// <param name="chosen">The Primary chosen for the Run. One of <paramref name="all"/>.</param>
        /// <param name="carryAll">Whether to carry every Primary, for the debug switch.</param>
        /// <param name="equippedIndex">Where the chosen Primary is among the carried ones: the one to equip.</param>
        /// <returns>The carried Primaries, in switching order.</returns>
        public static T[] For<T>(IReadOnlyList<T> all, T chosen, bool carryAll, out int equippedIndex) where T : class
        {
            if (all == null)
            {
                throw new ArgumentNullException(nameof(all));
            }

            if (chosen == null)
            {
                throw new ArgumentNullException(nameof(chosen));
            }

            var chosenIndex = -1;
            for (var i = 0; i < all.Count; i++)
            {
                if (ReferenceEquals(all[i], chosen))
                {
                    chosenIndex = i;
                    break;
                }
            }

            if (chosenIndex < 0)
            {
                throw new ArgumentException("The chosen Primary isn't one of the Primaries there are.", nameof(chosen));
            }

            if (!carryAll)
            {
                equippedIndex = 0;
                return new[] { chosen };
            }

            var carried = new T[all.Count];
            for (var i = 0; i < carried.Length; i++)
            {
                carried[i] = all[i];
            }

            equippedIndex = chosenIndex;
            return carried;
        }
    }
}
