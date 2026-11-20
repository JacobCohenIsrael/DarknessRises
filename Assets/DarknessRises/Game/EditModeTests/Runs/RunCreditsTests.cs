using System;
using System.Collections.Generic;
using DarknessRises.Game.Runs;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Runs
{
    public sealed class RunCreditsTests
    {
        [Test]
        public void A_new_Run_has_earned_nothing()
        {
            var credits = new RunCredits();

            Assert.That(credits.Earned, Is.Zero);
            Assert.That(credits.AdBonus, Is.Zero);
            Assert.That(credits.Total, Is.Zero);
            Assert.That(credits.Unbanked, Is.Zero);
            Assert.That(credits.HasAdBonus, Is.False);
        }

        [Test]
        public void Kills_and_cleared_Levels_add_up()
        {
            var credits = new RunCredits();

            credits.Earn(5);
            credits.Earn(8);
            credits.Earn(100);

            Assert.That(credits.Earned, Is.EqualTo(113));
            Assert.That(credits.Total, Is.EqualTo(113));
        }

        [Test]
        public void Credits_are_only_ever_earned()
        {
            var credits = new RunCredits();

            Assert.That(() => credits.Earn(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => credits.AddAdBonus(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void The_ad_bonus_comes_on_top_and_only_once()
        {
            var credits = new RunCredits();
            credits.Earn(40);

            Assert.That(credits.AddAdBonus(40), Is.True);
            Assert.That(credits.AddAdBonus(40), Is.False);

            Assert.That(credits.Earned, Is.EqualTo(40));
            Assert.That(credits.AdBonus, Is.EqualTo(40));
            Assert.That(credits.Total, Is.EqualTo(80));
            Assert.That(credits.HasAdBonus, Is.True);
        }

        [Test]
        public void Taking_the_unbanked_Credits_never_takes_one_twice()
        {
            var credits = new RunCredits();
            credits.Earn(30);

            Assert.That(credits.TakeUnbanked(), Is.EqualTo(30));
            Assert.That(credits.TakeUnbanked(), Is.Zero);

            credits.Earn(12);
            credits.AddAdBonus(42);

            Assert.That(credits.Unbanked, Is.EqualTo(54));
            Assert.That(credits.TakeUnbanked(), Is.EqualTo(54));
            Assert.That(credits.Banked, Is.EqualTo(84));
            Assert.That(credits.Unbanked, Is.Zero);
        }

        [Test]
        public void It_tells_when_the_total_changes()
        {
            var credits = new RunCredits();
            var totals = new List<int>();
            credits.Changed += totals.Add;

            credits.Earn(5);
            credits.Earn(0);
            credits.AddAdBonus(5);

            Assert.That(totals, Is.EqualTo(new[] { 5, 10 }));
        }
    }
}
