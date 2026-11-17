using System;
using DarknessRises.Game.Grenades;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Grenades
{
    public sealed class GrenadeChargesTests
    {
        private const int MaxCharges = 3;
        private const float RefillSeconds = 20f;

        private GrenadeCharges charges;
        private int changes;

        [SetUp]
        public void SetUp()
        {
            charges = new GrenadeCharges(new GrenadeChargeStats(MaxCharges, RefillSeconds));
            changes = 0;
            charges.ChargesChanged += () => changes++;
        }

        [Test]
        public void StartsWithEveryCharge()
        {
            Assert.That(charges.Charges, Is.EqualTo(MaxCharges));
            Assert.That(charges.MaxCharges, Is.EqualTo(MaxCharges));
            Assert.That(charges.IsFull, Is.True);
            Assert.That(charges.CanThrow, Is.True);
            Assert.That(charges.RefillProgress, Is.EqualTo(0f));
        }

        [Test]
        public void SpendingTakesOneCharge()
        {
            Assert.That(charges.TrySpend(), Is.True);

            Assert.That(charges.Charges, Is.EqualTo(MaxCharges - 1));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void CannotThrowWithNoChargesLeft()
        {
            SpendAll();

            Assert.That(charges.CanThrow, Is.False);
            Assert.That(charges.TrySpend(), Is.False);
            Assert.That(charges.Charges, Is.EqualTo(0));
            Assert.That(changes, Is.EqualTo(MaxCharges));
        }

        [Test]
        public void FullChargesDoNotBankRefillTime()
        {
            charges.Tick(RefillSeconds * 3f);
            charges.TrySpend();

            Assert.That(charges.Charges, Is.EqualTo(MaxCharges - 1));
            Assert.That(charges.RefillProgress, Is.EqualTo(0f));
        }

        [Test]
        public void AMissingChargeRefillsAfterTheRefillTime()
        {
            charges.TrySpend();

            charges.Tick(RefillSeconds - 1f);
            Assert.That(charges.Charges, Is.EqualTo(MaxCharges - 1));
            Assert.That(charges.RefillProgress, Is.EqualTo((RefillSeconds - 1f) / RefillSeconds).Within(1e-5f));

            charges.Tick(1f);
            Assert.That(charges.Charges, Is.EqualTo(MaxCharges));
            Assert.That(charges.RefillProgress, Is.EqualTo(0f));
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void ChargesRefillOneAtATime()
        {
            SpendAll();

            charges.Tick(RefillSeconds);
            Assert.That(charges.Charges, Is.EqualTo(1));

            charges.Tick(RefillSeconds);
            Assert.That(charges.Charges, Is.EqualTo(2));
        }

        [Test]
        public void ALongTickRefillsSeveralChargesButNeverMoreThanTheMaximum()
        {
            SpendAll();

            charges.Tick(RefillSeconds * 2.5f);
            Assert.That(charges.Charges, Is.EqualTo(2));
            Assert.That(charges.RefillProgress, Is.EqualTo(0.5f).Within(1e-5f));

            charges.Tick(RefillSeconds * 10f);
            Assert.That(charges.Charges, Is.EqualTo(MaxCharges));
            Assert.That(charges.RefillProgress, Is.EqualTo(0f));
        }

        [Test]
        public void SpendingMidRefillKeepsTheRefillGoing()
        {
            charges.TrySpend();
            charges.Tick(RefillSeconds * 0.75f);

            charges.TrySpend();
            Assert.That(charges.RefillProgress, Is.EqualTo(0.75f).Within(1e-5f));

            charges.Tick(RefillSeconds * 0.25f);
            Assert.That(charges.Charges, Is.EqualTo(MaxCharges - 1));
            Assert.That(charges.RefillProgress, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void AnEmptyCharacterCanThrowAgainOnceAChargeRefills()
        {
            SpendAll();
            charges.Tick(RefillSeconds);

            Assert.That(charges.CanThrow, Is.True);
            Assert.That(charges.TrySpend(), Is.True);
            Assert.That(charges.Charges, Is.EqualTo(0));
        }

        [Test]
        public void TimeOnlyMovesForward()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => charges.Tick(-0.1f));
        }

        [Test]
        public void StatsRejectNonsense()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GrenadeChargeStats(0, RefillSeconds));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GrenadeChargeStats(MaxCharges, 0f));
        }

        private void SpendAll()
        {
            while (charges.TrySpend())
            {
            }
        }
    }
}
