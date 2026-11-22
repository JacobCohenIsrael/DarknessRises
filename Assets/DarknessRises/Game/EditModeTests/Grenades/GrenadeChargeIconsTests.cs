using DarknessRises.Game.Grenades;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Grenades
{
    public sealed class GrenadeChargeIconsTests
    {
        [Test]
        public void CarriedChargesAreFull()
        {
            Assert.That(GrenadeChargeIcons.Fill(0, 3, 0.4f), Is.EqualTo(1f));
            Assert.That(GrenadeChargeIcons.Fill(2, 3, 0.4f), Is.EqualTo(1f));
        }

        [Test]
        public void TheFirstSpentChargeShowsTheRefill()
        {
            Assert.That(GrenadeChargeIcons.Fill(3, 3, 0.4f), Is.EqualTo(0.4f));
        }

        [Test]
        public void ChargesAfterTheRefillingOneAreDimmed()
        {
            Assert.That(GrenadeChargeIcons.Fill(4, 3, 0.4f), Is.Zero);
        }

        [Test]
        public void ARefillOutOfRangeIsClamped()
        {
            Assert.That(GrenadeChargeIcons.Fill(1, 1, 1.5f), Is.EqualTo(1f));
            Assert.That(GrenadeChargeIcons.Fill(1, 1, -0.5f), Is.Zero);
        }

        [Test]
        public void OneChargeSpentShowsAllButTheLastFullAndTheLastRefilling()
        {
            var charges = new GrenadeCharges(new GrenadeChargeStats(3, 10f));
            charges.TrySpend();
            charges.Tick(2.5f);

            Assert.That(GrenadeChargeIcons.Fill(0, charges), Is.EqualTo(1f));
            Assert.That(GrenadeChargeIcons.Fill(1, charges), Is.EqualTo(1f));
            Assert.That(GrenadeChargeIcons.Fill(2, charges), Is.EqualTo(0.25f).Within(1e-4f));
        }

        [Test]
        public void FullChargesAreAllFull()
        {
            var charges = new GrenadeCharges(new GrenadeChargeStats(2, 10f));

            Assert.That(GrenadeChargeIcons.Fill(0, charges), Is.EqualTo(1f));
            Assert.That(GrenadeChargeIcons.Fill(1, charges), Is.EqualTo(1f));
        }
    }
}
