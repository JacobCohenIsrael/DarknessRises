using System;
using System.Collections.Generic;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// The Primary weapons the character carries through a Run and switches between: every one the player has unlocked,
    /// each with its own magazine, reload state and ammo reserve. Only the equipped one is ever ticked or fired.
    /// It lives as long as the Run: magazines, reserves and the equipped Weapon carry over from Level to Level.
    /// Plain C# (ADR 0003).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A holstered Primary is frozen as it was: its magazine stays where it was, and a reload it was in the middle of
    /// waits and carries on once it's equipped again, so switching never skips a reload. Switching releases the
    /// trigger of the Primary being holstered.
    /// </para>
    /// <para>
    /// A Primary that has run dry (nothing in its magazine or its reserve) can't be selected until its reserve is
    /// refilled. When the equipped one runs dry, the loadout switches on its own to the Primary that never does: the
    /// first one with unlimited ammo, the Pistol.
    /// </para>
    /// </remarks>
    public sealed class PrimaryWeaponLoadout
    {
        private readonly PrimaryWeapon[] weapons;
        private readonly Action<PrimaryWeapon> onRanDry;

        /// <param name="primaryStats">One entry per Primary, in switching order, each with unlimited ammo. The first is equipped.</param>
        public PrimaryWeaponLoadout(IReadOnlyList<PrimaryWeaponStats> primaryStats) : this(WithUnlimitedAmmo(primaryStats))
        {
        }

        /// <param name="carried">The Primaries carried, in switching order. The first is equipped.</param>
        public PrimaryWeaponLoadout(IReadOnlyList<PrimaryWeapon> carried)
        {
            if (carried == null || carried.Count == 0)
            {
                throw new ArgumentException("The character carries at least one Primary weapon.", nameof(carried));
            }

            weapons = new PrimaryWeapon[carried.Count];
            onRanDry = OnRanDry;
            for (var i = 0; i < weapons.Length; i++)
            {
                weapons[i] = carried[i] ?? throw new ArgumentNullException(nameof(carried));
                weapons[i].RanDry += onRanDry;
            }
        }

        /// <summary>Raised after a different Primary is equipped, by the player or by the loadout, with the newly equipped one.</summary>
        public event Action<PrimaryWeapon> Switched;

        /// <summary>Raised when a Primary runs dry, with its index, before the loadout switches away from it.</summary>
        public event Action<int> RanDry;

        public int Count => weapons.Length;
        public int EquippedIndex { get; private set; }
        public PrimaryWeapon Equipped => weapons[EquippedIndex];

        public PrimaryWeapon this[int index] => weapons[index];

        /// <summary>Whether the Primary at <paramref name="index"/> can be equipped: it has ammo in its magazine or its reserve.</summary>
        public bool CanSelect(int index)
        {
            return index >= 0 && index < weapons.Length && weapons[index].HasAmmo;
        }

        /// <summary>
        /// Equips the Primary at <paramref name="index"/>. Does nothing if it's already equipped, or if it has run dry.
        /// </summary>
        /// <returns>Whether the equipped Primary changed.</returns>
        public bool Switch(int index)
        {
            if (index < 0 || index >= weapons.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"The loadout holds {weapons.Length} Primary weapons.");
            }

            if (index == EquippedIndex || !weapons[index].HasAmmo)
            {
                return false;
            }

            Equip(index);
            return true;
        }

        /// <summary>Equips the next Primary after the equipped one that has ammo, wrapping round. Does nothing if there is no other.</summary>
        /// <returns>Whether the equipped Primary changed.</returns>
        public bool SwitchToNext() => Step(1);

        /// <summary>Equips the Primary before the equipped one that has ammo, wrapping round. Does nothing if there is no other.</summary>
        /// <returns>Whether the equipped Primary changed.</returns>
        public bool SwitchToPrevious() => Step(-1);

        /// <summary>
        /// Refills every Primary's reserve by <paramref name="share"/> of a full one (0.5 is half): a new Level, or a
        /// Continue. A Primary that had run dry can be selected again.
        /// </summary>
        public void Refill(float share)
        {
            for (var i = 0; i < weapons.Length; i++)
            {
                weapons[i].Reserve.Refill(share);
            }
        }

        /// <summary>
        /// Forgets whoever listened to the loadout, its Primaries and their reserves. The loadout lives as long as the
        /// Run, so the Level that showed it calls this as its scene goes: nothing of that Level is held on to, or called
        /// again from the next one.
        /// </summary>
        public void DropListeners()
        {
            Switched = null;
            RanDry = null;
            for (var i = 0; i < weapons.Length; i++)
            {
                weapons[i].DropListeners();
                weapons[i].RanDry += onRanDry;
            }
        }

        /// <summary>Advances the equipped Primary by <paramref name="deltaSeconds"/>. Holstered ones stay frozen.</summary>
        public void Tick(float deltaSeconds) => Tick(deltaSeconds, mayFire: true);

        /// <summary>
        /// Advances the equipped Primary by <paramref name="deltaSeconds"/>, holding its fire when <paramref name="mayFire"/>
        /// is false (the switch beat). Holstered ones stay frozen.
        /// </summary>
        public void Tick(float deltaSeconds, bool mayFire)
        {
            Equipped.Tick(deltaSeconds, mayFire);
        }

        private bool Step(int direction)
        {
            for (var step = 1; step < weapons.Length; step++)
            {
                var index = ((EquippedIndex + direction * step) % weapons.Length + weapons.Length) % weapons.Length;
                if (weapons[index].HasAmmo)
                {
                    Equip(index);
                    return true;
                }
            }

            return false;
        }

        private void Equip(int index)
        {
            Equipped.ReleaseTrigger();
            EquippedIndex = index;
            Switched?.Invoke(Equipped);
        }

        // The equipped Primary fired its last round: the Primary that never runs dry takes over.
        private void OnRanDry(PrimaryWeapon weapon)
        {
            var index = Array.IndexOf(weapons, weapon);
            RanDry?.Invoke(index);
            if (index != EquippedIndex)
            {
                return;
            }

            var fallback = Fallback();
            if (fallback >= 0)
            {
                Equip(fallback);
            }
        }

        // The first Primary with unlimited ammo; failing that, the first one with any ammo left.
        private int Fallback()
        {
            for (var i = 0; i < weapons.Length; i++)
            {
                if (weapons[i].Reserve.IsUnlimited)
                {
                    return i;
                }
            }

            for (var i = 0; i < weapons.Length; i++)
            {
                if (weapons[i].HasAmmo)
                {
                    return i;
                }
            }

            return -1;
        }

        private static PrimaryWeapon[] WithUnlimitedAmmo(IReadOnlyList<PrimaryWeaponStats> primaryStats)
        {
            if (primaryStats == null || primaryStats.Count == 0)
            {
                throw new ArgumentException("The character carries at least one Primary weapon.", nameof(primaryStats));
            }

            var carried = new PrimaryWeapon[primaryStats.Count];
            for (var i = 0; i < carried.Length; i++)
            {
                carried[i] = new PrimaryWeapon(primaryStats[i]);
            }

            return carried;
        }
    }
}
