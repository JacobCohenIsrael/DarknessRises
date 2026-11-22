using System;
using System.Collections.Generic;
using DarknessRises.Game.Configuration;
using DarknessRises.Game.Shop;
using DarknessRises.Game.Weapons;

namespace DarknessRises.Game.Upgrades
{
    /// <summary>
    /// Reads the numbers Upgrades can change out of the authored definitions: each one's starting value
    /// (<see cref="BaseValue"/>, which the Shop shows before any level is bought), and a whole Run's effective stats
    /// (<see cref="For"/>), which hands those starting values and the player's levels to <see cref="RunStatsBuilder"/>.
    /// It only copies numbers out; the rules are in the builder.
    /// </summary>
    public static class ConfiguredRunStats
    {
        /// <summary>
        /// The Run's effective stats: the definitions' numbers with every Upgrade applied at the level
        /// <paramref name="keeper"/> has it.
        /// </summary>
        /// <param name="carried">The Primaries the character carries, in switching order.</param>
        public static RunStats For(
            GameConfiguration configuration, ShopCatalog catalog, ShopKeeper keeper, IReadOnlyList<WeaponDefinition> carried)
        {
            Func<ShopItem, int> levelOf = keeper.LevelOf;
            var primaries = new WeaponRunStats[carried.Count];
            for (var i = 0; i < primaries.Length; i++)
            {
                var weapon = carried[i];
                primaries[i] = RunStatsBuilder.Weapon(
                    weapon.PrimaryStats, weapon.Damage, weapon.ReserveRounds, UpgradesOf(catalog, weapon, levelOf));
            }

            var grenade = configuration.Grenade;
            var skills = new OwnedUpgrades(ShopCatalog.UpgradesOf(catalog.Skills), levelOf);
            var character = new OwnedUpgrades(ShopCatalog.UpgradesOf(catalog.Character), levelOf);
            return new RunStats(
                primaries,
                RunStatsBuilder.Grenade(
                    grenade.ChargeStats, grenade.BlastRadius, new OwnedUpgrades(ShopCatalog.UpgradesOf(catalog.Grenade), levelOf)),
                RunStatsBuilder.Flare(configuration.Flare.Timing, skills),
                RunStatsBuilder.Adrenaline(configuration.Adrenaline.Timing, skills),
                RunStatsBuilder.Lure(configuration.Lure.Timing, skills),
                RunStatsBuilder.MaxHitPoints(configuration.Player.HitPoints, character),
                RunStatsBuilder.AuraRange(configuration.Player.Aura.Range, character));
        }

        /// <summary>
        /// The value <paramref name="stat"/> has with no Upgrade bought: the one authored on its own definition.
        /// <paramref name="weapon"/> is the Weapon a Weapon's number belongs to; other numbers ignore it.
        /// </summary>
        public static float BaseValue(UpgradeStat stat, GameConfiguration configuration, WeaponDefinition weapon)
        {
            switch (stat)
            {
                case UpgradeStat.Damage:
                    return Require(weapon, stat).Damage;
                case UpgradeStat.MagazineSize:
                    return Require(weapon, stat).MagazineSize;
                case UpgradeStat.ReloadSeconds:
                    return Require(weapon, stat).ReloadSeconds;
                case UpgradeStat.ShotsPerSecond:
                    return Require(weapon, stat).ShotsPerSecond;
                case UpgradeStat.AmmoReserve:
                    return Require(weapon, stat).ReserveRounds;
                case UpgradeStat.GrenadeCharges:
                    return configuration.Grenade.ChargeStats.MaxCharges;
                case UpgradeStat.GrenadeRefillSeconds:
                    return configuration.Grenade.ChargeStats.RefillSeconds;
                case UpgradeStat.GrenadeBlastRadius:
                    return configuration.Grenade.BlastRadius;
                case UpgradeStat.FlareDurationSeconds:
                    return configuration.Flare.Timing.DurationSeconds;
                case UpgradeStat.FlareCooldownSeconds:
                    return configuration.Flare.Timing.CooldownSeconds;
                case UpgradeStat.AdrenalineDurationSeconds:
                    return configuration.Adrenaline.Timing.DurationSeconds;
                case UpgradeStat.AdrenalineCooldownSeconds:
                    return configuration.Adrenaline.Timing.CooldownSeconds;
                case UpgradeStat.LureDurationSeconds:
                    return configuration.Lure.Timing.DurationSeconds;
                case UpgradeStat.LureCooldownSeconds:
                    return configuration.Lure.Timing.CooldownSeconds;
                case UpgradeStat.MaxHitPoints:
                    return configuration.Player.HitPoints;
                case UpgradeStat.AuraRange:
                    return configuration.Player.Aura.Range;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stat), stat, "No definition holds this number.");
            }
        }

        // The Upgrades the Shop sells for this Weapon. A Weapon the Shop doesn't sell has none.
        private static OwnedUpgrades UpgradesOf(ShopCatalog catalog, WeaponDefinition weapon, Func<ShopItem, int> levelOf)
        {
            for (var i = 0; i < catalog.Weapons.Count; i++)
            {
                if (catalog.Weapons[i].Offer.Weapon == weapon)
                {
                    return new OwnedUpgrades(ShopCatalog.UpgradesOf(catalog.Weapons[i].Upgrades), levelOf);
                }
            }

            return OwnedUpgrades.None;
        }

        private static WeaponDefinition Require(WeaponDefinition weapon, UpgradeStat stat)
        {
            return weapon ? weapon : throw new ArgumentNullException(nameof(weapon), $"{stat} is a Weapon's number: it needs the Weapon.");
        }
    }
}
