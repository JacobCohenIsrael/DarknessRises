using System;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// The numbers a <see cref="PrimaryWeapon"/> needs, copied out of a <see cref="WeaponDefinition"/>
    /// so the rule never holds on to authored data.
    /// </summary>
    public readonly struct PrimaryWeaponStats
    {
        public PrimaryWeaponStats(float shotsPerSecond, int magazineSize, float reloadSeconds)
        {
            if (shotsPerSecond <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(shotsPerSecond), shotsPerSecond, "A Weapon must fire at a positive rate.");
            }

            if (magazineSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(magazineSize), magazineSize, "A magazine holds at least one shot.");
            }

            if (reloadSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(reloadSeconds), reloadSeconds, "A reload can't take negative time.");
            }

            ShotsPerSecond = shotsPerSecond;
            MagazineSize = magazineSize;
            ReloadSeconds = reloadSeconds;
        }

        public float ShotsPerSecond { get; }
        public int MagazineSize { get; }
        public float ReloadSeconds { get; }

        /// <summary>Seconds between two shots while the trigger is held.</summary>
        public float SecondsBetweenShots => 1f / ShotsPerSecond;
    }
}
