using System;
using System.Collections.Generic;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Saving;
using DarknessRises.Game.Shop;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Shop
{
    public sealed class ShopKeeperTests
    {
        private MemorySaveStorage storage;
        private PlayerSave save;
        private ShopKeeper keeper;

        // The Shop's three Primaries: one owned from the start, two to unlock.
        private ShopItem pistol;
        private ShopItem machineGun;
        private ShopItem flamethrower;
        private ShopItem[] weapons;

        // An Upgrade with three levels, each dearer than the last.
        private ShopItem magazine;

        [SetUp]
        public void SetUp()
        {
            storage = new MemorySaveStorage();
            save = PlayerSave.Load(storage);
            keeper = new ShopKeeper(save);
            pistol = new ShopItem("pistol", Array.Empty<int>(), startLevel: 1);
            machineGun = new ShopItem("machine-gun", new[] { 2500 });
            flamethrower = new ShopItem("flamethrower", new[] { 6000 });
            weapons = new[] { pistol, machineGun, flamethrower };
            magazine = new ShopItem("pistol.magazine", new[] { 250, 500, 900 });
        }

        private void Earn(int credits)
        {
            var runCredits = new RunCredits();
            runCredits.Earn(credits);
            save.Bank(runCredits);
        }

        // An app restart: a new save read from the same storage, and a new keeper over it.
        private ShopKeeper AfterRestart() => new(PlayerSave.Load(storage));

        [Test]
        public void Nothing_is_bought_at_first_except_what_is_owned_from_the_start()
        {
            Assert.That(keeper.LevelOf(pistol), Is.EqualTo(1));
            Assert.That(keeper.Owns(pistol), Is.True);
            Assert.That(keeper.IsMaxed(pistol), Is.True);

            Assert.That(keeper.LevelOf(machineGun), Is.Zero);
            Assert.That(keeper.Owns(machineGun), Is.False);
            Assert.That(keeper.LevelOf(magazine), Is.Zero);
            Assert.That(keeper.NextPrice(magazine), Is.EqualTo(250));
        }

        [Test]
        public void An_item_the_bank_cant_cover_cant_be_bought()
        {
            Earn(249);

            Assert.That(keeper.CanBuy(magazine), Is.False);
            Assert.That(keeper.Buy(magazine), Is.False);

            Assert.That(keeper.LevelOf(magazine), Is.Zero);
            Assert.That(keeper.Credits, Is.EqualTo(249));
        }

        [Test]
        public void Buying_takes_the_Credits_and_raises_the_level()
        {
            Earn(300);

            Assert.That(keeper.CanBuy(magazine), Is.True);
            Assert.That(keeper.Buy(magazine), Is.True);

            Assert.That(keeper.LevelOf(magazine), Is.EqualTo(1));
            Assert.That(keeper.Credits, Is.EqualTo(50));
            Assert.That(keeper.NextPrice(magazine), Is.EqualTo(500));
        }

        [Test]
        public void An_exact_price_is_enough()
        {
            Earn(250);

            Assert.That(keeper.Buy(magazine), Is.True);
            Assert.That(keeper.Credits, Is.Zero);
        }

        [Test]
        public void Levels_are_bought_one_at_a_time_each_at_its_own_price()
        {
            Earn(250 + 500 + 900);

            Assert.That(keeper.Buy(magazine), Is.True);
            Assert.That(keeper.Buy(magazine), Is.True);
            Assert.That(keeper.Buy(magazine), Is.True);

            Assert.That(keeper.LevelOf(magazine), Is.EqualTo(3));
            Assert.That(keeper.Credits, Is.Zero);
        }

        [Test]
        public void A_maxed_item_cant_be_bought_however_rich_the_bank()
        {
            Earn(100000);
            keeper.Buy(magazine);
            keeper.Buy(magazine);
            keeper.Buy(magazine);
            var credits = keeper.Credits;

            Assert.That(keeper.IsMaxed(magazine), Is.True);
            Assert.That(keeper.CanBuy(magazine), Is.False);
            Assert.That(keeper.NextPrice(magazine), Is.Zero);
            Assert.That(keeper.Buy(magazine), Is.False);
            Assert.That(keeper.Credits, Is.EqualTo(credits));
            Assert.That(keeper.LevelOf(magazine), Is.EqualTo(3));
        }

        [Test]
        public void What_is_bought_survives_an_app_restart()
        {
            Earn(3000);
            keeper.Buy(magazine);
            keeper.Buy(machineGun);

            var restarted = AfterRestart();

            Assert.That(restarted.LevelOf(magazine), Is.EqualTo(1));
            Assert.That(restarted.Owns(machineGun), Is.True);
            Assert.That(restarted.Credits, Is.EqualTo(250));
        }

        [Test]
        public void Buying_saves_at_once()
        {
            Earn(300);
            var writes = storage.WriteCount;

            keeper.Buy(magazine);

            Assert.That(storage.WriteCount, Is.EqualTo(writes + 1));
        }

        [Test]
        public void It_tells_what_was_bought()
        {
            Earn(300);
            var bought = new List<ShopItem>();
            keeper.Bought += bought.Add;

            keeper.Buy(magazine);
            keeper.Buy(magazine);

            Assert.That(bought, Is.EqualTo(new[] { magazine }));
        }

        [Test]
        public void A_level_saved_above_an_items_top_reads_as_maxed()
        {
            save.Purchase(magazine.Id, 9, 0);

            Assert.That(keeper.LevelOf(magazine), Is.EqualTo(3));
            Assert.That(keeper.IsMaxed(magazine), Is.True);
        }

        [Test]
        public void Unlocking_a_Weapon_takes_its_price_once()
        {
            Earn(2600);

            Assert.That(keeper.Buy(machineGun), Is.True);

            Assert.That(keeper.Owns(machineGun), Is.True);
            Assert.That(keeper.IsMaxed(machineGun), Is.True);
            Assert.That(keeper.Credits, Is.EqualTo(100));
            Assert.That(keeper.Buy(machineGun), Is.False);
        }

        [Test]
        public void The_Weapon_owned_from_the_start_is_the_Primary_until_another_is_chosen()
        {
            Assert.That(keeper.PrimaryOf(weapons), Is.SameAs(pistol));
        }

        [Test]
        public void A_Weapon_that_isnt_owned_cant_be_chosen()
        {
            Assert.That(keeper.ChoosePrimary(machineGun), Is.False);

            Assert.That(keeper.PrimaryOf(weapons), Is.SameAs(pistol));
        }

        [Test]
        public void An_owned_Weapon_can_be_chosen_as_the_Primary_and_the_choice_is_saved()
        {
            Earn(2500);
            keeper.Buy(machineGun);

            Assert.That(keeper.PrimaryOf(weapons), Is.SameAs(pistol), "Unlocking alone doesn't change the Primary.");
            Assert.That(keeper.ChoosePrimary(machineGun), Is.True);

            Assert.That(keeper.PrimaryOf(weapons), Is.SameAs(machineGun));
            Assert.That(AfterRestart().PrimaryOf(weapons).Id, Is.EqualTo("machine-gun"));
        }

        [Test]
        public void The_player_can_go_back_to_the_starting_Weapon()
        {
            Earn(2500);
            keeper.Buy(machineGun);
            keeper.ChoosePrimary(machineGun);

            Assert.That(keeper.ChoosePrimary(pistol), Is.True);

            Assert.That(keeper.PrimaryOf(weapons), Is.SameAs(pistol));
        }

        [Test]
        public void Choosing_the_Primary_again_changes_nothing()
        {
            var chosen = new List<ShopItem>();
            keeper.PrimaryChosen += chosen.Add;
            Earn(2500);
            keeper.Buy(machineGun);

            Assert.That(keeper.ChoosePrimary(machineGun), Is.True);
            Assert.That(keeper.ChoosePrimary(machineGun), Is.False);

            Assert.That(chosen, Is.EqualTo(new[] { machineGun }));
        }

        [Test]
        public void A_saved_choice_the_Shop_no_longer_sells_falls_back_to_the_starting_Weapon()
        {
            save.ChoosePrimaryWeapon("railgun");

            Assert.That(keeper.PrimaryOf(weapons), Is.SameAs(pistol));
        }

        [Test]
        public void One_Primary_must_be_owned_from_the_start()
        {
            var locked = new[] { machineGun, flamethrower };

            Assert.That(() => keeper.PrimaryOf(locked), Throws.InstanceOf<InvalidOperationException>());
        }
    }
}
