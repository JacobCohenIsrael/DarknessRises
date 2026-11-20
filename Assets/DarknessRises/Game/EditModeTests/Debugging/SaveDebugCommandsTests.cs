using DarknessRises.Game.Boot;
using DarknessRises.Game.Debugging;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Saving;
using DarknessRises.Game.Shop;
using DarknessRises.Game.Upgrades;
using DarknessRises.Game.Weapons;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Debugging
{
    /// <summary>The dev tools that reach any state of the save: add Credits, max out everything, reset.</summary>
    public sealed class SaveDebugCommandsTests
    {
        private const int CreditsPerPress = 1000;

        private MemorySaveStorage storage;
        private PlayerSave save;
        private ShopDefinition shop;
        private WeaponDefinition pistol;
        private WeaponDefinition machineGun;
        private SaveDebugCommands commands;

        [SetUp]
        public void SetUp()
        {
            storage = new MemorySaveStorage();
            save = PlayerSave.Load(storage);
            pistol = ScriptableObject.CreateInstance<WeaponDefinition>();
            machineGun = ScriptableObject.CreateInstance<WeaponDefinition>();
            shop = ScriptableObject.CreateInstance<ShopDefinition>();

            // A Shop with a Weapon owned from the start, one to unlock with an Upgrade of its own, and a Grenade Upgrade.
            var serialized = new SerializedObject(shop);
            serialized.FindProperty("debugCredits").intValue = CreditsPerPress;
            var weapons = serialized.FindProperty("weapons");
            weapons.arraySize = 2;
            AuthorWeapon(weapons.GetArrayElementAtIndex(0), "pistol", pistol, 0);
            AuthorWeapon(weapons.GetArrayElementAtIndex(1), "machine-gun", machineGun, 2500);
            AuthorUpgrade(weapons.GetArrayElementAtIndex(1).FindPropertyRelative("upgrades"), "machine-gun.magazine", UpgradeStat.MagazineSize, 3);
            AuthorUpgrade(serialized.FindProperty("grenadeUpgrades"), "grenade.charges", UpgradeStat.GrenadeCharges, 2);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            commands = new SaveDebugCommands(save, shop);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(shop);
            Object.DestroyImmediate(pistol);
            Object.DestroyImmediate(machineGun);
        }

        private static void AuthorWeapon(SerializedProperty offer, string id, WeaponDefinition weapon, int price)
        {
            offer.FindPropertyRelative("id").stringValue = id;
            offer.FindPropertyRelative("weapon").objectReferenceValue = weapon;
            offer.FindPropertyRelative("price").intValue = price;
        }

        private static void AuthorUpgrade(SerializedProperty upgrades, string id, UpgradeStat stat, int levelCount)
        {
            upgrades.arraySize = 1;
            var upgrade = upgrades.GetArrayElementAtIndex(0);
            upgrade.FindPropertyRelative("id").stringValue = id;
            upgrade.FindPropertyRelative("stat").intValue = (int)stat;
            var levels = upgrade.FindPropertyRelative("levels");
            levels.arraySize = levelCount;
            for (var i = 0; i < levelCount; i++)
            {
                levels.GetArrayElementAtIndex(i).FindPropertyRelative("price").intValue = 100 * (i + 1);
                levels.GetArrayElementAtIndex(i).FindPropertyRelative("value").floatValue = 10 * (i + 1);
            }
        }

        // An app restart: a new save read from the same storage.
        private PlayerSave Restarted() => PlayerSave.Load(storage);

        [Test]
        public void Add_Credits_puts_the_authored_amount_in_the_bank_and_saves()
        {
            commands.AddCredits();
            commands.AddCredits();

            Assert.That(commands.CreditsPerPress, Is.EqualTo(CreditsPerPress));
            Assert.That(save.Credits, Is.EqualTo(2 * CreditsPerPress));
            Assert.That(Restarted().Credits, Is.EqualTo(2 * CreditsPerPress));
        }

        [Test]
        public void Max_out_unlocks_every_Weapon_and_maxes_every_Upgrade_for_free()
        {
            commands.AddCredits();

            commands.MaxOut();

            var catalog = shop.ToCatalog();
            var keeper = new ShopKeeper(save);
            foreach (var item in catalog.AllItems)
            {
                Assert.That(keeper.IsMaxed(item), Is.True, item.Id);
            }

            Assert.That(keeper.Owns(catalog.WeaponItems[1]), Is.True);
            Assert.That(save.LevelOf("machine-gun.magazine"), Is.EqualTo(3));
            Assert.That(save.LevelOf("grenade.charges"), Is.EqualTo(2));
            Assert.That(save.Credits, Is.EqualTo(CreditsPerPress), "It costs nothing.");
        }

        [Test]
        public void Max_out_keeps_the_chosen_Primary_and_survives_an_app_restart()
        {
            commands.MaxOut();
            var catalog = shop.ToCatalog();

            Assert.That(new ShopKeeper(save).PrimaryOf(catalog.WeaponItems), Is.SameAs(catalog.WeaponItems[0]));

            var restarted = Restarted();
            Assert.That(restarted.LevelOf("machine-gun"), Is.EqualTo(1));
            Assert.That(restarted.LevelOf("machine-gun.magazine"), Is.EqualTo(3));
        }

        [Test]
        public void Max_out_twice_changes_nothing_more()
        {
            commands.MaxOut();
            var writes = storage.WriteCount;

            commands.MaxOut();

            Assert.That(storage.WriteCount, Is.EqualTo(writes));
            Assert.That(save.LevelOf("machine-gun.magazine"), Is.EqualTo(3));
        }

        [Test]
        public void Reset_puts_the_save_back_to_a_first_install()
        {
            var earned = new RunCredits();
            earned.Earn(300);
            save.FinishRun(earned);
            commands.MaxOut();
            save.ChoosePrimaryWeapon("machine-gun");
            Assert.That(BootDestinations.For(save), Is.EqualTo(BootDestination.Shop));

            commands.ResetSave();

            foreach (var read in new[] { save, Restarted() })
            {
                Assert.That(read.Credits, Is.Zero);
                Assert.That(read.RunsFinished, Is.Zero);
                Assert.That(read.LevelOf("machine-gun"), Is.Zero);
                Assert.That(read.LevelOf("machine-gun.magazine"), Is.Zero);
                Assert.That(read.PrimaryWeaponId, Is.Empty);
                Assert.That(BootDestinations.For(read), Is.EqualTo(BootDestination.FirstRun), "The next launch goes straight into Level 1.");
            }
        }

        [Test]
        public void An_open_Shop_hears_of_every_command()
        {
            var keeper = new ShopKeeper(save);
            var changes = 0;
            keeper.Changed += () => changes++;

            commands.AddCredits();
            Assert.That(changes, Is.GreaterThan(0));

            changes = 0;
            commands.MaxOut();
            Assert.That(changes, Is.GreaterThan(0));

            changes = 0;
            commands.ResetSave();
            Assert.That(changes, Is.GreaterThan(0));
        }
    }
}
