using System;
using DarknessRises.Game.Credits;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Credits
{
    public sealed class LevelClearBonusTests
    {
        [Test]
        public void The_first_Level_pays_its_authored_bonus()
        {
            var bonus = new LevelClearBonus(firstLevelCredits: 100, growthPerLevel: 50, maxCredits: 500);

            Assert.That(bonus.For(1), Is.EqualTo(100));
        }

        [Test]
        public void Each_Level_after_it_pays_more()
        {
            var bonus = new LevelClearBonus(firstLevelCredits: 100, growthPerLevel: 50, maxCredits: 500);

            Assert.That(bonus.For(2), Is.EqualTo(150));
            Assert.That(bonus.For(3), Is.EqualTo(200));
            Assert.That(bonus.For(5), Is.EqualTo(300));
        }

        [Test]
        public void It_stops_growing_at_the_cap()
        {
            var bonus = new LevelClearBonus(firstLevelCredits: 100, growthPerLevel: 50, maxCredits: 500);

            Assert.That(bonus.For(9), Is.EqualTo(500));
            Assert.That(bonus.For(10), Is.EqualTo(500));
            Assert.That(bonus.For(int.MaxValue), Is.EqualTo(500));
        }

        [Test]
        public void A_cap_below_the_first_Levels_bonus_doesnt_cut_it()
        {
            var bonus = new LevelClearBonus(firstLevelCredits: 100, growthPerLevel: 50, maxCredits: 0);

            Assert.That(bonus.For(1), Is.EqualTo(100));
            Assert.That(bonus.For(7), Is.EqualTo(100));
        }

        [Test]
        public void Without_growth_every_Level_pays_the_same()
        {
            var bonus = new LevelClearBonus(firstLevelCredits: 80, growthPerLevel: 0, maxCredits: 500);

            Assert.That(bonus.For(1), Is.EqualTo(80));
            Assert.That(bonus.For(12), Is.EqualTo(80));
        }

        [Test]
        public void Levels_count_from_one_and_nothing_is_negative()
        {
            var bonus = new LevelClearBonus(100, 50, 500);

            Assert.That(() => bonus.For(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new LevelClearBonus(-1, 50, 500), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new LevelClearBonus(100, -1, 500), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}
