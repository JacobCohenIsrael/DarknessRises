using DarknessRises.Game.Grenades;
using DarknessRises.Game.Skills;
using DarknessRises.Game.Weapons;

namespace DarknessRises.Game.Upgrades
{
    /// <summary>
    /// Builds each part of a Run's effective stats: the authored numbers, copied out of their definition, with the
    /// owner's Upgrades applied at the levels the player owns. A number without an Upgrade, or with an Upgrade the
    /// player hasn't bought, stays as authored. Plain C# (ADR 0003).
    /// </summary>
    public static class RunStatsBuilder
    {
        /// <summary>A Primary weapon's numbers: fire rate, magazine, reload and damage, as its Upgrades set them.</summary>
        public static WeaponRunStats Weapon(PrimaryWeaponStats firing, int damage, OwnedUpgrades upgrades)
        {
            return new WeaponRunStats(
                new PrimaryWeaponStats(
                    upgrades.Value(UpgradeStat.ShotsPerSecond, firing.ShotsPerSecond),
                    upgrades.Whole(UpgradeStat.MagazineSize, firing.MagazineSize),
                    upgrades.Value(UpgradeStat.ReloadSeconds, firing.ReloadSeconds)),
                upgrades.Whole(UpgradeStat.Damage, damage));
        }

        /// <summary>The Grenade's numbers: charges, refill time and blast radius, as its Upgrades set them.</summary>
        public static GrenadeRunStats Grenade(GrenadeChargeStats charges, float blastRadius, OwnedUpgrades upgrades)
        {
            return new GrenadeRunStats(
                new GrenadeChargeStats(
                    upgrades.Whole(UpgradeStat.GrenadeCharges, charges.MaxCharges),
                    upgrades.Value(UpgradeStat.GrenadeRefillSeconds, charges.RefillSeconds)),
                upgrades.Value(UpgradeStat.GrenadeBlastRadius, blastRadius));
        }

        /// <summary>The Flare's duration and cooldown, as the Skills' Upgrades set them.</summary>
        public static SkillTiming Flare(SkillTiming timing, OwnedUpgrades upgrades)
        {
            return new SkillTiming(
                upgrades.Value(UpgradeStat.FlareDurationSeconds, timing.DurationSeconds),
                upgrades.Value(UpgradeStat.FlareCooldownSeconds, timing.CooldownSeconds));
        }

        /// <summary>Slow-mo's duration and cooldown, as the Skills' Upgrades set them.</summary>
        public static SkillTiming SlowMo(SkillTiming timing, OwnedUpgrades upgrades)
        {
            return new SkillTiming(
                upgrades.Value(UpgradeStat.SlowMoDurationSeconds, timing.DurationSeconds),
                upgrades.Value(UpgradeStat.SlowMoCooldownSeconds, timing.CooldownSeconds));
        }

        /// <summary>The hit points the character starts a Run with, as the character's Upgrades set them.</summary>
        public static int MaxHitPoints(int hitPoints, OwnedUpgrades upgrades)
        {
            return upgrades.Whole(UpgradeStat.MaxHitPoints, hitPoints);
        }

        /// <summary>How far the Aura's light reaches, as the character's Upgrades set it.</summary>
        public static float AuraRange(float range, OwnedUpgrades upgrades)
        {
            return upgrades.Value(UpgradeStat.AuraRange, range);
        }
    }
}
