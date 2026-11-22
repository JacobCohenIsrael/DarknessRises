using DarknessRises.Game.Weapons;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Weapons
{
    public sealed class MuzzleFlashStrengthTests
    {
        [Test]
        public void FadesFromFullToNothingOverItsDuration()
        {
            Assert.That(MuzzleFlashSettings.Strength(0.1f, 0.1f, 0f, 0.5f), Is.EqualTo(1f));
            Assert.That(MuzzleFlashSettings.Strength(0.05f, 0.1f, 0f, 0.5f), Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(MuzzleFlashSettings.Strength(0f, 0.1f, 0f, 0.5f), Is.Zero);
        }

        [Test]
        public void AFlickerOnlyEverTakesAway()
        {
            Assert.That(MuzzleFlashSettings.Strength(0.1f, 0.1f, 0.6f, 0f), Is.EqualTo(1f));
            Assert.That(MuzzleFlashSettings.Strength(0.1f, 0.1f, 0.6f, 1f), Is.EqualTo(0.4f).Within(1e-6f));
        }

        [Test]
        public void TheLightNeverGoesOutBeforeTheFlashEnds()
        {
            // So Gunfire lights as far, for as long, whatever the flicker.
            Assert.That(MuzzleFlashSettings.Strength(0.01f, 0.1f, 1f, 1f), Is.GreaterThan(0f));
        }
    }
}
