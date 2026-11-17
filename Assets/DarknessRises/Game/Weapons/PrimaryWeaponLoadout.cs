using System;
using System.Collections.Generic;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// The Primary weapons the character can switch between, each with its own magazine and reload state.
    /// Only the equipped one is ever ticked or fired. Plain C# (ADR 0003).
    /// </summary>
    /// <remarks>
    /// A holstered Primary is frozen as it was: its magazine stays where it was, and a reload it was in the middle of
    /// waits and carries on once it's equipped again, so switching never skips a reload. Switching releases the
    /// trigger of the Primary being holstered.
    /// </remarks>
    public sealed class PrimaryWeaponLoadout
    {
        private readonly PrimaryWeapon[] weapons;

        /// <param name="primaryStats">One entry per Primary, in switching order. The first is equipped.</param>
        public PrimaryWeaponLoadout(IReadOnlyList<PrimaryWeaponStats> primaryStats)
        {
            if (primaryStats == null || primaryStats.Count == 0)
            {
                throw new ArgumentException("The character carries at least one Primary weapon.", nameof(primaryStats));
            }

            weapons = new PrimaryWeapon[primaryStats.Count];
            for (var i = 0; i < weapons.Length; i++)
            {
                weapons[i] = new PrimaryWeapon(primaryStats[i]);
            }
        }

        /// <summary>Raised after a different Primary is equipped, with the newly equipped one.</summary>
        public event Action<PrimaryWeapon> Switched;

        public int Count => weapons.Length;
        public int EquippedIndex { get; private set; }
        public PrimaryWeapon Equipped => weapons[EquippedIndex];

        public PrimaryWeapon this[int index] => weapons[index];

        /// <summary>Equips the Primary at <paramref name="index"/>. Does nothing if it's already equipped.</summary>
        /// <returns>Whether the equipped Primary changed.</returns>
        public bool Switch(int index)
        {
            if (index < 0 || index >= weapons.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"The loadout holds {weapons.Length} Primary weapons.");
            }

            if (index == EquippedIndex)
            {
                return false;
            }

            Equipped.ReleaseTrigger();
            EquippedIndex = index;
            Switched?.Invoke(Equipped);
            return true;
        }

        /// <summary>Advances the equipped Primary by <paramref name="deltaSeconds"/>. Holstered ones stay frozen.</summary>
        public void Tick(float deltaSeconds)
        {
            Equipped.Tick(deltaSeconds);
        }
    }
}
