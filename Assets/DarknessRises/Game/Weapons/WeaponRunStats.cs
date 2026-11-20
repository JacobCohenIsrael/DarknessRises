using System;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// A Primary weapon's numbers for one Run: its authored ones with the player's Upgrades applied. The Weapon's rule
    /// and its Shots read these, never the definition's own numbers.
    /// </summary>
    public readonly struct WeaponRunStats
    {
        /// <summary>The reserve rounds of a Weapon whose ammo never runs out.</summary>
        public const int UnlimitedAmmo = -1;

        /// <param name="firing">The fire rate, magazine and reload the Weapon's rule runs on.</param>
        /// <param name="damage">Damage one Shot deals.</param>
        /// <param name="reserveRounds">The rounds a full ammo reserve holds, or <see cref="UnlimitedAmmo"/>.</param>
        public WeaponRunStats(PrimaryWeaponStats firing, int damage, int reserveRounds = UnlimitedAmmo)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), damage, "A Shot can't heal what it hits.");
            }

            Firing = firing;
            Damage = damage;
            ReserveRounds = reserveRounds < 0 ? UnlimitedAmmo : reserveRounds;
        }

        /// <summary>The fire rate, magazine and reload the Weapon's rule runs on.</summary>
        public PrimaryWeaponStats Firing { get; }

        /// <summary>Damage one Shot deals to the Enemy it hits.</summary>
        public int Damage { get; }

        /// <summary>The rounds a full ammo reserve holds. Means nothing when the ammo is unlimited.</summary>
        public int ReserveRounds { get; }

        /// <summary>Whether the Weapon reloads forever, as the Pistol does.</summary>
        public bool HasUnlimitedAmmo => ReserveRounds < 0;

        /// <summary>A fresh Weapon with these numbers: a full magazine and a full reserve.</summary>
        public PrimaryWeapon ToWeapon()
        {
            return new PrimaryWeapon(Firing, HasUnlimitedAmmo ? AmmoReserve.Unlimited() : new AmmoReserve(ReserveRounds));
        }
    }
}
