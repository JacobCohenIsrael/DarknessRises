using System;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// A Primary weapon's numbers for one Run: its authored ones with the player's Upgrades applied. The Weapon's rule
    /// and its Shots read these, never the definition's own numbers.
    /// </summary>
    public readonly struct WeaponRunStats
    {
        public WeaponRunStats(PrimaryWeaponStats firing, int damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), damage, "A Shot can't heal what it hits.");
            }

            Firing = firing;
            Damage = damage;
        }

        /// <summary>The fire rate, magazine and reload the Weapon's rule runs on.</summary>
        public PrimaryWeaponStats Firing { get; }

        /// <summary>Damage one Shot deals to the Enemy it hits.</summary>
        public int Damage { get; }
    }
}
