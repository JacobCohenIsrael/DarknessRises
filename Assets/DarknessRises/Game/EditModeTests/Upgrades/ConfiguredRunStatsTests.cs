using System;
using System.Collections.Generic;
using DarknessRises.Game.Configuration;
using DarknessRises.Game.Grenades;
using DarknessRises.Game.Player;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Saving;
using DarknessRises.Game.Shop;
using DarknessRises.Game.Skills;
using DarknessRises.Game.Upgrades;
using DarknessRises.Game.Weapons;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DarknessRises.Game.EditModeTests.Upgrades
{
    /// <summary>
    /// The wiring from the authored definitions to a Run's stats: with a Shop definition that has one Upgrade for every
    /// number, buying an Upgrade changes that number of the Run and no other, and every other number is the one its
    /// definition authored. The definitions are throwaway instances with their default tuning.
    /// </summary>
    public sealed class ConfiguredRunStatsTests
    {
        // The value each Upgrade's single level sets: unlike any definition's default, and valid for its number.
        private static readonly Dictionary<UpgradeStat, float> Bought = new()
        {
            { UpgradeStat.Damage, 7f },
            { UpgradeStat.MagazineSize, 77f },
            { UpgradeStat.ReloadSeconds, 0.5f },
            { UpgradeStat.ShotsPerSecond, 9f },
            { UpgradeStat.AmmoReserve, 444f },
            { UpgradeStat.GrenadeCharges, 6f },
            { UpgradeStat.GrenadeRefillSeconds, 9f },
            { UpgradeStat.GrenadeBlastRadius, 8f },
            { UpgradeStat.FlareDurationSeconds, 9f },
            { UpgradeStat.FlareCooldownSeconds, 3f },
            { UpgradeStat.SlowMoDurationSeconds, 8f },
            { UpgradeStat.SlowMoCooldownSeconds, 2f },
            { UpgradeStat.MaxHitPoints, 9f },
            { UpgradeStat.AuraRange, 6.5f },
        };

        private static readonly UpgradeStat[] WeaponStats =
        {
            UpgradeStat.Damage, UpgradeStat.MagazineSize, UpgradeStat.ReloadSeconds, UpgradeStat.ShotsPerSecond,
            UpgradeStat.AmmoReserve,
        };

        private static readonly UpgradeStat[] GrenadeStats =
        {
            UpgradeStat.GrenadeCharges, UpgradeStat.GrenadeRefillSeconds, UpgradeStat.GrenadeBlastRadius,
        };

        private static readonly UpgradeStat[] SkillStats =
        {
            UpgradeStat.FlareDurationSeconds, UpgradeStat.FlareCooldownSeconds,
            UpgradeStat.SlowMoDurationSeconds, UpgradeStat.SlowMoCooldownSeconds,
        };

        private static readonly UpgradeStat[] CharacterStats = { UpgradeStat.MaxHitPoints, UpgradeStat.AuraRange };

        private const int UpgradePrice = 10;

        // The test Weapon's ammo runs out, so its reserve is a number an Upgrade can raise.
        private const int AuthoredReserve = 200;

        private readonly List<Object> created = new();
        private GameConfiguration configuration;
        private WeaponDefinition weapon;

        [SetUp]
        public void SetUp()
        {
            weapon = Create<WeaponDefinition>();
            var serializedWeapon = new SerializedObject(weapon);
            serializedWeapon.FindProperty("hasUnlimitedAmmo").boolValue = false;
            serializedWeapon.FindProperty("ammoReserve").intValue = AuthoredReserve;
            serializedWeapon.ApplyModifiedPropertiesWithoutUndo();
            var shop = Create<ShopDefinition>();
            var serializedShop = new SerializedObject(shop);
            var weapons = serializedShop.FindProperty("weapons");
            weapons.arraySize = 1;
            var offer = weapons.GetArrayElementAtIndex(0);
            offer.FindPropertyRelative("id").stringValue = "weapon";
            offer.FindPropertyRelative("weapon").objectReferenceValue = weapon;
            offer.FindPropertyRelative("price").intValue = 0;
            Author(offer.FindPropertyRelative("upgrades"), WeaponStats);
            Author(serializedShop.FindProperty("grenadeUpgrades"), GrenadeStats);
            Author(serializedShop.FindProperty("skillUpgrades"), SkillStats);
            Author(serializedShop.FindProperty("characterUpgrades"), CharacterStats);
            serializedShop.ApplyModifiedPropertiesWithoutUndo();

            configuration = Create<GameConfiguration>();
            var serialized = new SerializedObject(configuration);
            serialized.FindProperty("player").objectReferenceValue = Create<PlayerDefinition>();
            serialized.FindProperty("grenade").objectReferenceValue = Create<GrenadeDefinition>();
            serialized.FindProperty("flare").objectReferenceValue = Create<FlareDefinition>();
            serialized.FindProperty("slowMo").objectReferenceValue = Create<SlowMoDefinition>();
            serialized.FindProperty("shop").objectReferenceValue = shop;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in created)
            {
                Object.DestroyImmediate(asset);
            }

            created.Clear();
        }

        private T Create<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            created.Add(asset);
            return asset;
        }

        // One Upgrade per stat, with one level that sets the stat's bought value.
        private static void Author(SerializedProperty upgrades, UpgradeStat[] stats)
        {
            upgrades.arraySize = stats.Length;
            for (var i = 0; i < stats.Length; i++)
            {
                var upgrade = upgrades.GetArrayElementAtIndex(i);
                upgrade.FindPropertyRelative("id").stringValue = stats[i].ToString();
                upgrade.FindPropertyRelative("stat").intValue = (int)stats[i];
                var levels = upgrade.FindPropertyRelative("levels");
                levels.arraySize = 1;
                levels.GetArrayElementAtIndex(0).FindPropertyRelative("price").intValue = UpgradePrice;
                levels.GetArrayElementAtIndex(0).FindPropertyRelative("value").floatValue = Bought[stats[i]];
            }
        }

        private static ShopKeeper KeeperWith(int credits)
        {
            var save = PlayerSave.Load(new MemorySaveStorage());
            var earned = new RunCredits();
            earned.Earn(credits);
            save.Bank(earned);
            return new ShopKeeper(save);
        }

        private static ShopItem ItemOf(ShopCatalog catalog, UpgradeStat stat)
        {
            foreach (var item in catalog.AllItems)
            {
                if (item.Id == stat.ToString())
                {
                    return item;
                }
            }

            throw new ArgumentException($"The Shop has no Upgrade for {stat}.");
        }

        // The number of a Run that an Upgrade of this stat sets.
        private static float Read(RunStats stats, UpgradeStat stat)
        {
            return stat switch
            {
                UpgradeStat.Damage => stats.Primaries[0].Damage,
                UpgradeStat.MagazineSize => stats.Primaries[0].Firing.MagazineSize,
                UpgradeStat.ReloadSeconds => stats.Primaries[0].Firing.ReloadSeconds,
                UpgradeStat.ShotsPerSecond => stats.Primaries[0].Firing.ShotsPerSecond,
                UpgradeStat.AmmoReserve => stats.Primaries[0].ReserveRounds,
                UpgradeStat.GrenadeCharges => stats.Grenade.Charges.MaxCharges,
                UpgradeStat.GrenadeRefillSeconds => stats.Grenade.Charges.RefillSeconds,
                UpgradeStat.GrenadeBlastRadius => stats.Grenade.BlastRadius,
                UpgradeStat.FlareDurationSeconds => stats.Flare.DurationSeconds,
                UpgradeStat.FlareCooldownSeconds => stats.Flare.CooldownSeconds,
                UpgradeStat.SlowMoDurationSeconds => stats.SlowMo.DurationSeconds,
                UpgradeStat.SlowMoCooldownSeconds => stats.SlowMo.CooldownSeconds,
                UpgradeStat.MaxHitPoints => stats.MaxHitPoints,
                UpgradeStat.AuraRange => stats.AuraRange,
                _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null),
            };
        }

        [Test]
        public void With_nothing_bought_every_number_is_the_one_its_definition_authored()
        {
            var catalog = configuration.Shop.ToCatalog();

            var stats = ConfiguredRunStats.For(configuration, catalog, KeeperWith(0), new[] { weapon });

            foreach (UpgradeStat stat in Enum.GetValues(typeof(UpgradeStat)))
            {
                Assert.That(Read(stats, stat), Is.EqualTo(ConfiguredRunStats.BaseValue(stat, configuration, weapon)), stat.ToString());
            }

            // The definitions' defaults, so the test would notice a number read from the wrong place.
            Assert.That(stats.Primaries[0].Firing.MagazineSize, Is.EqualTo(weapon.MagazineSize));
            Assert.That(stats.Primaries[0].ReserveRounds, Is.EqualTo(AuthoredReserve));
            Assert.That(stats.Grenade.Charges.MaxCharges, Is.EqualTo(configuration.Grenade.ChargeStats.MaxCharges));
            Assert.That(stats.MaxHitPoints, Is.EqualTo(configuration.Player.HitPoints));
            Assert.That(stats.AuraRange, Is.EqualTo(configuration.Player.Aura.Range));
        }

        [Test]
        public void Buying_an_Upgrade_changes_its_number_of_the_next_Run_and_no_other([Values] UpgradeStat bought)
        {
            var catalog = configuration.Shop.ToCatalog();
            var keeper = KeeperWith(UpgradePrice);

            Assert.That(keeper.Buy(ItemOf(catalog, bought)), Is.True);
            var stats = ConfiguredRunStats.For(configuration, catalog, keeper, new[] { weapon });

            foreach (UpgradeStat stat in Enum.GetValues(typeof(UpgradeStat)))
            {
                var expected = stat == bought ? Bought[stat] : ConfiguredRunStats.BaseValue(stat, configuration, weapon);
                Assert.That(Read(stats, stat), Is.EqualTo(expected), stat.ToString());
            }
        }

        [Test]
        public void The_definitions_are_never_changed()
        {
            var catalog = configuration.Shop.ToCatalog();
            var keeper = KeeperWith(1000);
            var magazine = weapon.MagazineSize;
            var hitPoints = configuration.Player.HitPoints;
            foreach (var item in catalog.AllItems)
            {
                keeper.Buy(item);
            }

            ConfiguredRunStats.For(configuration, catalog, keeper, new[] { weapon });

            Assert.That(weapon.MagazineSize, Is.EqualTo(magazine));
            Assert.That(configuration.Player.HitPoints, Is.EqualTo(hitPoints));
        }

        [Test]
        public void A_Weapon_the_Shop_doesnt_sell_is_carried_as_authored()
        {
            var other = Create<WeaponDefinition>();
            var catalog = configuration.Shop.ToCatalog();
            var keeper = KeeperWith(1000);
            keeper.Buy(ItemOf(catalog, UpgradeStat.MagazineSize));

            var stats = ConfiguredRunStats.For(configuration, catalog, keeper, new[] { weapon, other });

            Assert.That(stats.Primaries[0].Firing.MagazineSize, Is.EqualTo(77));
            Assert.That(stats.Primaries[1].Firing.MagazineSize, Is.EqualTo(other.MagazineSize));
        }

        [Test]
        public void A_Weapon_authored_with_unlimited_ammo_is_carried_with_unlimited_ammo()
        {
            // A definition's ammo is unlimited until a reserve is authored for it: the Pistol's.
            var pistol = Create<WeaponDefinition>();
            var catalog = configuration.Shop.ToCatalog();

            var stats = ConfiguredRunStats.For(configuration, catalog, KeeperWith(0), new[] { pistol, weapon });

            Assert.That(stats.Primaries[0].HasUnlimitedAmmo, Is.True);
            Assert.That(stats.Primaries[1].HasUnlimitedAmmo, Is.False);
            Assert.That(stats.Primaries[1].ReserveRounds, Is.EqualTo(AuthoredReserve));
        }

        [Test]
        public void A_Weapons_number_needs_the_Weapon()
        {
            Assert.That(
                () => ConfiguredRunStats.BaseValue(UpgradeStat.MagazineSize, configuration, null),
                Throws.InstanceOf<ArgumentNullException>());
            Assert.That(ConfiguredRunStats.BaseValue(UpgradeStat.MaxHitPoints, configuration, null), Is.EqualTo(configuration.Player.HitPoints));
        }
    }
}
