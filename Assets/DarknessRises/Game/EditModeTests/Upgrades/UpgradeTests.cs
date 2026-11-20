using System;
using DarknessRises.Game.Shop;
using DarknessRises.Game.Upgrades;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Upgrades
{
    public sealed class UpgradeTests
    {
        private static Upgrade Magazine()
        {
            return new Upgrade(new ShopItem("pistol.magazine", new[] { 250, 500, 900 }), UpgradeStat.MagazineSize, new[] { 15f, 18f, 22f });
        }

        [Test]
        public void Without_a_level_the_number_is_the_definitions_own()
        {
            Assert.That(Magazine().ValueAt(0, 12f), Is.EqualTo(12f));
        }

        [Test]
        public void Each_level_sets_the_number_to_its_value_whatever_it_started_at()
        {
            var magazine = Magazine();

            Assert.That(magazine.ValueAt(1, 12f), Is.EqualTo(15f));
            Assert.That(magazine.ValueAt(2, 12f), Is.EqualTo(18f));
            Assert.That(magazine.ValueAt(3, 12f), Is.EqualTo(22f));
            Assert.That(magazine.ValueAt(3, 1000f), Is.EqualTo(22f));
        }

        [Test]
        public void A_level_above_the_top_counts_as_the_top()
        {
            Assert.That(Magazine().ValueAt(9, 12f), Is.EqualTo(22f));
        }

        [Test]
        public void A_value_can_go_down_as_a_reload_does()
        {
            var reload = new Upgrade(new ShopItem("pistol.reload", new[] { 300, 600 }), UpgradeStat.ReloadSeconds, new[] { 1f, 0.85f });

            Assert.That(reload.ValueAt(0, 1.2f), Is.EqualTo(1.2f));
            Assert.That(reload.ValueAt(2, 1.2f), Is.EqualTo(0.85f));
        }

        [Test]
        public void It_has_a_level_for_each_value_and_sells_exactly_those()
        {
            var magazine = Magazine();

            Assert.That(magazine.MaxLevel, Is.EqualTo(3));
            Assert.That(magazine.Item.MaxLevel, Is.EqualTo(3));
            Assert.That(magazine.Stat, Is.EqualTo(UpgradeStat.MagazineSize));
        }

        [Test]
        public void Its_prices_and_values_must_match_level_for_level()
        {
            var twoPrices = new ShopItem("x", new[] { 100, 200 });
            var ownedFromTheStart = new ShopItem("y", new[] { 100 }, startLevel: 1);

            Assert.That(() => new Upgrade(twoPrices, UpgradeStat.Damage, new[] { 2f }), Throws.InstanceOf<ArgumentException>());
            Assert.That(() => new Upgrade(ownedFromTheStart, UpgradeStat.Damage, new[] { 2f }), Throws.InstanceOf<ArgumentException>());
            Assert.That(() => new Upgrade(null, UpgradeStat.Damage, new[] { 2f }), Throws.InstanceOf<ArgumentNullException>());
            Assert.That(() => new Upgrade(twoPrices, UpgradeStat.Damage, null), Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void Owned_Upgrades_answer_each_number_at_the_players_level()
        {
            var magazine = Magazine();
            var owned = new OwnedUpgrades(new[] { magazine }, item => item == magazine.Item ? 2 : 0);

            Assert.That(owned.Value(UpgradeStat.MagazineSize, 12f), Is.EqualTo(18f));
            Assert.That(owned.Whole(UpgradeStat.MagazineSize, 12), Is.EqualTo(18));
            Assert.That(owned.Value(UpgradeStat.ReloadSeconds, 1.2f), Is.EqualTo(1.2f), "No Upgrade for it: as authored.");
            Assert.That(OwnedUpgrades.None.Whole(UpgradeStat.MagazineSize, 12), Is.EqualTo(12));
        }

        [Test]
        public void A_whole_number_is_rounded_to_the_nearest()
        {
            var charges = new Upgrade(new ShopItem("grenade.charges", new[] { 600 }), UpgradeStat.GrenadeCharges, new[] { 3.5f });
            var owned = new OwnedUpgrades(new[] { charges }, _ => 1);

            Assert.That(owned.Whole(UpgradeStat.GrenadeCharges, 3), Is.EqualTo(4));
        }
    }
}
