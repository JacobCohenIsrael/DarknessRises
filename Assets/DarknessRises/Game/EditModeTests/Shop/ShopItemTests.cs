using System;
using DarknessRises.Game.Shop;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Shop
{
    public sealed class ShopItemTests
    {
        [Test]
        public void An_Upgrade_has_a_level_for_each_price()
        {
            var item = new ShopItem("pistol.magazine", new[] { 250, 500, 900 });

            Assert.That(item.StartLevel, Is.Zero);
            Assert.That(item.MaxLevel, Is.EqualTo(3));
            Assert.That(item.PriceOf(1), Is.EqualTo(250));
            Assert.That(item.PriceOf(2), Is.EqualTo(500));
            Assert.That(item.PriceOf(3), Is.EqualTo(900));
        }

        [Test]
        public void A_Weapon_to_unlock_has_one_level()
        {
            var item = new ShopItem("machine-gun", new[] { 2500 });

            Assert.That(item.MaxLevel, Is.EqualTo(1));
            Assert.That(item.PriceOf(1), Is.EqualTo(2500));
        }

        [Test]
        public void An_item_owned_from_the_start_has_nothing_to_buy()
        {
            var item = new ShopItem("pistol", Array.Empty<int>(), startLevel: 1);

            Assert.That(item.StartLevel, Is.EqualTo(1));
            Assert.That(item.MaxLevel, Is.EqualTo(1));
            Assert.That(() => item.PriceOf(1), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Only_the_levels_it_sells_have_a_price()
        {
            var item = new ShopItem("pistol.magazine", new[] { 250, 500 });

            Assert.That(() => item.PriceOf(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => item.PriceOf(3), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void It_keeps_its_own_copy_of_the_prices()
        {
            var prices = new[] { 250, 500 };
            var item = new ShopItem("pistol.magazine", prices);

            prices[0] = 1;

            Assert.That(item.PriceOf(1), Is.EqualTo(250));
        }

        [Test]
        public void It_needs_an_id_and_prices_that_arent_negative()
        {
            Assert.That(() => new ShopItem(null, new[] { 1 }), Throws.InstanceOf<ArgumentException>());
            Assert.That(() => new ShopItem(string.Empty, new[] { 1 }), Throws.InstanceOf<ArgumentException>());
            Assert.That(() => new ShopItem("x", null), Throws.InstanceOf<ArgumentNullException>());
            Assert.That(() => new ShopItem("x", new[] { 10, -1 }), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new ShopItem("x", new[] { 10 }, startLevel: -1), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}
