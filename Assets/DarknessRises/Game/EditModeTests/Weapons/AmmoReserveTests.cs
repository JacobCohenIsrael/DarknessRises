using System;
using DarknessRises.Game.Weapons;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Weapons
{
    /// <summary>The ammo a Primary weapon carries beyond its magazine: spending it and refilling it.</summary>
    public sealed class AmmoReserveTests
    {
        private AmmoReserve reserve;
        private int changes;

        [SetUp]
        public void SetUp()
        {
            reserve = new AmmoReserve(240);
            changes = 0;
            reserve.Changed += () => changes++;
        }

        [Test]
        public void It_starts_full()
        {
            Assert.That(reserve.Rounds, Is.EqualTo(240));
            Assert.That(reserve.Max, Is.EqualTo(240));
            Assert.That(reserve.IsEmpty, Is.False);
            Assert.That(reserve.IsUnlimited, Is.False);
        }

        [Test]
        public void A_reload_takes_what_it_wants_while_there_is_enough()
        {
            Assert.That(reserve.Take(30), Is.EqualTo(30));

            Assert.That(reserve.Rounds, Is.EqualTo(210));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void A_reload_that_wants_more_than_is_left_gets_what_is_left()
        {
            reserve.Take(220);

            Assert.That(reserve.Take(30), Is.EqualTo(20));

            Assert.That(reserve.Rounds, Is.Zero);
            Assert.That(reserve.IsEmpty, Is.True);
        }

        [Test]
        public void An_empty_reserve_gives_nothing_and_tells_no_one()
        {
            reserve.Take(240);
            changes = 0;

            Assert.That(reserve.Take(30), Is.Zero);
            Assert.That(reserve.Take(0), Is.Zero);

            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void A_refill_puts_back_a_share_of_a_full_reserve()
        {
            reserve.Take(240);
            changes = 0;

            Assert.That(reserve.Refill(0.5f), Is.EqualTo(120));

            Assert.That(reserve.Rounds, Is.EqualTo(120));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void A_refill_never_goes_above_full()
        {
            reserve.Take(60);

            Assert.That(reserve.Refill(0.5f), Is.EqualTo(60), "Half would be 120, and only 60 are missing.");
            Assert.That(reserve.Refill(1f), Is.Zero);

            Assert.That(reserve.Rounds, Is.EqualTo(240));
        }

        [Test]
        public void A_full_refill_fills_it_from_anywhere()
        {
            reserve.Take(199);

            reserve.Refill(1f);

            Assert.That(reserve.Rounds, Is.EqualTo(240));
        }

        [Test]
        public void A_share_that_isnt_whole_rounds_is_rounded()
        {
            var small = new AmmoReserve(5);
            small.Take(5);

            Assert.That(small.Refill(0.5f), Is.EqualTo(3), "Half of 5 rounds up.");
            Assert.That(small.Rounds, Is.EqualTo(3));
        }

        [Test]
        public void A_refill_of_nothing_changes_nothing()
        {
            reserve.Take(100);
            changes = 0;

            Assert.That(reserve.Refill(0f), Is.Zero);

            Assert.That(reserve.Rounds, Is.EqualTo(140));
            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void An_unlimited_reserve_never_runs_out()
        {
            var unlimited = AmmoReserve.Unlimited();
            var told = 0;
            unlimited.Changed += () => told++;

            Assert.That(unlimited.IsUnlimited, Is.True);
            Assert.That(unlimited.Take(1000), Is.EqualTo(1000));
            Assert.That(unlimited.Take(1000), Is.EqualTo(1000));
            Assert.That(unlimited.IsEmpty, Is.False);
            Assert.That(unlimited.Refill(1f), Is.Zero);
            Assert.That(told, Is.Zero);
        }

        [Test]
        public void A_reserve_can_be_authored_empty()
        {
            var none = new AmmoReserve(0);

            Assert.That(none.IsEmpty, Is.True);
            Assert.That(none.Refill(1f), Is.Zero);
            Assert.That(none.IsEmpty, Is.True);
        }

        [Test]
        public void Dropped_listeners_hear_nothing_more()
        {
            reserve.DropListeners();

            reserve.Take(10);

            Assert.That(changes, Is.Zero);
            Assert.That(reserve.Rounds, Is.EqualTo(230));
        }

        [Test]
        public void Sizes_takes_and_refills_are_never_negative()
        {
            Assert.That(() => new AmmoReserve(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => reserve.Take(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => reserve.Refill(-0.5f), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => reserve.Refill(float.NaN), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}
