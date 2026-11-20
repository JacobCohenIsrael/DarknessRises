using System;
using System.Collections.Generic;
using DarknessRises.Game.Grenades;
using DarknessRises.Game.Shop;
using DarknessRises.Game.Skills;
using DarknessRises.Game.Upgrades;
using DarknessRises.Game.Weapons;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Upgrades
{
    /// <summary>Building a Run's effective stats from the authored numbers and the Upgrades the player owns.</summary>
    public sealed class RunStatsBuilderTests
    {
        // The levels the player owns, by item id. Anything else is at level 0.
        private Dictionary<string, int> levels;

        // The Pistol as authored: 4 Shots a second, 12 in the magazine, a 1.2 second reload, 1 damage.
        private static readonly PrimaryWeaponStats Pistol = new(4f, 12, 1.2f);
        private const int PistolDamage = 1;

        [SetUp]
        public void SetUp()
        {
            levels = new Dictionary<string, int>();
        }

        private static Upgrade NewUpgrade(string id, UpgradeStat stat, params float[] values)
        {
            var prices = new int[values.Length];
            for (var i = 0; i < prices.Length; i++)
            {
                prices[i] = 100 * (i + 1);
            }

            return new Upgrade(new ShopItem(id, prices), stat, values);
        }

        private OwnedUpgrades Owned(params Upgrade[] upgrades)
        {
            return new OwnedUpgrades(upgrades, item => levels.TryGetValue(item.Id, out var level) ? level : 0);
        }

        private OwnedUpgrades PistolUpgrades()
        {
            return Owned(
                NewUpgrade("pistol.damage", UpgradeStat.Damage, 2f, 3f),
                NewUpgrade("pistol.magazine", UpgradeStat.MagazineSize, 15f, 18f, 22f),
                NewUpgrade("pistol.reload", UpgradeStat.ReloadSeconds, 1f, 0.85f, 0.7f),
                NewUpgrade("pistol.fire-rate", UpgradeStat.ShotsPerSecond, 5f, 6f, 7f));
        }

        [Test]
        public void With_nothing_bought_a_Weapon_is_as_authored()
        {
            var stats = RunStatsBuilder.Weapon(Pistol, PistolDamage, PistolUpgrades());

            Assert.That(stats.Firing.ShotsPerSecond, Is.EqualTo(4f));
            Assert.That(stats.Firing.MagazineSize, Is.EqualTo(12));
            Assert.That(stats.Firing.ReloadSeconds, Is.EqualTo(1.2f));
            Assert.That(stats.Damage, Is.EqualTo(1));
        }

        [Test]
        public void A_bought_level_sets_its_number_and_leaves_the_others()
        {
            levels["pistol.magazine"] = 1;

            var stats = RunStatsBuilder.Weapon(Pistol, PistolDamage, PistolUpgrades());

            Assert.That(stats.Firing.MagazineSize, Is.EqualTo(15));
            Assert.That(stats.Firing.ShotsPerSecond, Is.EqualTo(4f));
            Assert.That(stats.Firing.ReloadSeconds, Is.EqualTo(1.2f));
            Assert.That(stats.Damage, Is.EqualTo(1));
        }

        [Test]
        public void Each_level_sets_the_number_to_its_own_value()
        {
            levels["pistol.magazine"] = 3;
            levels["pistol.reload"] = 2;
            levels["pistol.fire-rate"] = 1;
            levels["pistol.damage"] = 2;

            var stats = RunStatsBuilder.Weapon(Pistol, PistolDamage, PistolUpgrades());

            Assert.That(stats.Firing.MagazineSize, Is.EqualTo(22));
            Assert.That(stats.Firing.ReloadSeconds, Is.EqualTo(0.85f));
            Assert.That(stats.Firing.ShotsPerSecond, Is.EqualTo(5f));
            Assert.That(stats.Damage, Is.EqualTo(3));
        }

        [Test]
        public void A_Weapon_without_an_Upgrade_for_a_number_keeps_it()
        {
            // The Flamethrower has no fire rate Upgrade.
            var flamethrower = new PrimaryWeaponStats(15f, 60, 2.5f);
            levels["flamethrower.tank"] = 1;
            var upgrades = Owned(NewUpgrade("flamethrower.tank", UpgradeStat.MagazineSize, 80f, 100f));

            var stats = RunStatsBuilder.Weapon(flamethrower, 1, upgrades);

            Assert.That(stats.Firing.MagazineSize, Is.EqualTo(80));
            Assert.That(stats.Firing.ShotsPerSecond, Is.EqualTo(15f));
        }

        [Test]
        public void One_Weapons_Upgrades_dont_reach_another_Weapon()
        {
            levels["pistol.magazine"] = 3;
            var machineGun = new PrimaryWeaponStats(10f, 30, 2.5f);
            var machineGunUpgrades = Owned(NewUpgrade("machine-gun.magazine", UpgradeStat.MagazineSize, 40f, 50f));

            Assert.That(RunStatsBuilder.Weapon(Pistol, PistolDamage, PistolUpgrades()).Firing.MagazineSize, Is.EqualTo(22));
            Assert.That(RunStatsBuilder.Weapon(machineGun, 1, machineGunUpgrades).Firing.MagazineSize, Is.EqualTo(30));
        }

        [Test]
        public void The_Grenades_charges_refill_and_blast_follow_their_Upgrades()
        {
            var authored = new GrenadeChargeStats(3, 20f);
            var upgrades = Owned(
                NewUpgrade("grenade.charges", UpgradeStat.GrenadeCharges, 4f, 5f),
                NewUpgrade("grenade.refill", UpgradeStat.GrenadeRefillSeconds, 17f, 14f, 11f),
                NewUpgrade("grenade.blast", UpgradeStat.GrenadeBlastRadius, 4f, 4.5f, 5f));

            var before = RunStatsBuilder.Grenade(authored, 3.5f, upgrades);
            levels["grenade.charges"] = 2;
            levels["grenade.refill"] = 1;
            levels["grenade.blast"] = 3;
            var after = RunStatsBuilder.Grenade(authored, 3.5f, upgrades);

            Assert.That(before.Charges.MaxCharges, Is.EqualTo(3));
            Assert.That(before.Charges.RefillSeconds, Is.EqualTo(20f));
            Assert.That(before.BlastRadius, Is.EqualTo(3.5f));
            Assert.That(after.Charges.MaxCharges, Is.EqualTo(5));
            Assert.That(after.Charges.RefillSeconds, Is.EqualTo(17f));
            Assert.That(after.BlastRadius, Is.EqualTo(5f));
        }

        [Test]
        public void Each_Skill_has_its_own_duration_and_cooldown_Upgrades()
        {
            var authored = new SkillTiming(4f, 15f);
            var upgrades = Owned(
                NewUpgrade("flare.duration", UpgradeStat.FlareDurationSeconds, 5f, 6f),
                NewUpgrade("flare.cooldown", UpgradeStat.FlareCooldownSeconds, 13f, 11f),
                NewUpgrade("slow-mo.duration", UpgradeStat.SlowMoDurationSeconds, 5f, 6f),
                NewUpgrade("slow-mo.cooldown", UpgradeStat.SlowMoCooldownSeconds, 13f, 11f));
            levels["flare.duration"] = 2;
            levels["slow-mo.cooldown"] = 1;

            var flare = RunStatsBuilder.Flare(authored, upgrades);
            var slowMo = RunStatsBuilder.SlowMo(authored, upgrades);

            Assert.That(flare.DurationSeconds, Is.EqualTo(6f));
            Assert.That(flare.CooldownSeconds, Is.EqualTo(15f));
            Assert.That(slowMo.DurationSeconds, Is.EqualTo(4f));
            Assert.That(slowMo.CooldownSeconds, Is.EqualTo(13f));
        }

        [Test]
        public void The_characters_hit_points_and_Aura_follow_their_Upgrades()
        {
            var upgrades = Owned(
                NewUpgrade("character.hit-points", UpgradeStat.MaxHitPoints, 6f, 7f, 8f),
                NewUpgrade("character.aura", UpgradeStat.AuraRange, 4.8f, 5.1f, 5.4f));

            Assert.That(RunStatsBuilder.MaxHitPoints(5, upgrades), Is.EqualTo(5));
            Assert.That(RunStatsBuilder.AuraRange(4.5f, upgrades), Is.EqualTo(4.5f));

            levels["character.hit-points"] = 3;
            levels["character.aura"] = 1;

            Assert.That(RunStatsBuilder.MaxHitPoints(5, upgrades), Is.EqualTo(8));
            Assert.That(RunStatsBuilder.AuraRange(4.5f, upgrades), Is.EqualTo(4.8f));
        }

        [Test]
        public void With_no_Upgrades_at_all_everything_is_as_authored()
        {
            var weapon = RunStatsBuilder.Weapon(Pistol, PistolDamage, OwnedUpgrades.None);
            var grenade = RunStatsBuilder.Grenade(new GrenadeChargeStats(3, 20f), 3.5f, OwnedUpgrades.None);

            Assert.That(weapon.Firing.MagazineSize, Is.EqualTo(12));
            Assert.That(weapon.Damage, Is.EqualTo(1));
            Assert.That(grenade.Charges.MaxCharges, Is.EqualTo(3));
            Assert.That(RunStatsBuilder.MaxHitPoints(5, OwnedUpgrades.None), Is.EqualTo(5));
            Assert.That(RunStatsBuilder.AuraRange(4.5f, OwnedUpgrades.None), Is.EqualTo(4.5f));
        }

        [Test]
        public void A_Runs_stats_hold_every_carried_Primary_in_order()
        {
            var pistol = RunStatsBuilder.Weapon(Pistol, PistolDamage, OwnedUpgrades.None);
            var machineGun = new WeaponRunStats(new PrimaryWeaponStats(10f, 30, 2.5f), 1);
            var grenade = new GrenadeRunStats(new GrenadeChargeStats(3, 20f), 3.5f);
            var timing = new SkillTiming(4f, 15f);

            var stats = new RunStats(new[] { pistol, machineGun }, grenade, timing, timing, 5, 4.5f);

            Assert.That(stats.Primaries.Count, Is.EqualTo(2));
            Assert.That(stats.Primaries[0].Firing.MagazineSize, Is.EqualTo(12));
            Assert.That(stats.Primaries[1].Firing.MagazineSize, Is.EqualTo(30));
            Assert.That(stats.MaxHitPoints, Is.EqualTo(5));
            Assert.That(stats.AuraRange, Is.EqualTo(4.5f));
        }

        [Test]
        public void A_Runs_stats_reject_what_no_Run_can_have()
        {
            var weapon = new WeaponRunStats(Pistol, 1);
            var grenade = new GrenadeRunStats(new GrenadeChargeStats(3, 20f), 3.5f);
            var timing = new SkillTiming(4f, 15f);

            Assert.That(() => new RunStats(Array.Empty<WeaponRunStats>(), grenade, timing, timing, 5, 4.5f), Throws.InstanceOf<ArgumentException>());
            Assert.That(() => new RunStats(new[] { weapon }, grenade, timing, timing, 0, 4.5f), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new RunStats(new[] { weapon }, grenade, timing, timing, 5, -1f), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new WeaponRunStats(Pistol, -1), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new GrenadeRunStats(new GrenadeChargeStats(3, 20f), 0f), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}
