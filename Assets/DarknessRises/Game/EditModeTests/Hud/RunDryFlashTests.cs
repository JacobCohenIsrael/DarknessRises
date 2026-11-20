using System;
using DarknessRises.Game.Hud;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Hud
{
    /// <summary>The flash on the Weapon button when a Primary weapon runs dry.</summary>
    public sealed class RunDryFlashTests
    {
        // Half a second, so the steps below are exact in floats.
        private const float Seconds = 0.5f;

        private RunDryFlash flash;

        [SetUp]
        public void SetUp()
        {
            flash = new RunDryFlash(Seconds);
        }

        [Test]
        public void Nothing_shows_until_a_Weapon_runs_dry()
        {
            flash.Tick(1f);

            Assert.That(flash.IsFlashing, Is.False);
            Assert.That(flash.Strength, Is.Zero);
        }

        [Test]
        public void It_shows_at_full_strength_as_the_Weapon_runs_dry()
        {
            flash.Start();

            Assert.That(flash.IsFlashing, Is.True);
            Assert.That(flash.Strength, Is.EqualTo(1f));
        }

        [Test]
        public void It_fades_evenly_over_its_time()
        {
            flash.Start();

            flash.Tick(0.125f);
            Assert.That(flash.Strength, Is.EqualTo(0.75f));

            flash.Tick(0.125f);
            Assert.That(flash.Strength, Is.EqualTo(0.5f));
            Assert.That(flash.IsFlashing, Is.True);
        }

        [Test]
        public void It_is_gone_once_its_time_is_up_and_stays_gone()
        {
            flash.Start();

            flash.Tick(Seconds);
            Assert.That(flash.IsFlashing, Is.False);
            Assert.That(flash.Strength, Is.Zero);

            flash.Tick(5f);
            Assert.That(flash.Strength, Is.Zero);
        }

        [Test]
        public void A_step_longer_than_what_is_left_ends_it_at_nothing()
        {
            flash.Start();
            flash.Tick(0.25f);

            flash.Tick(2f);

            Assert.That(flash.Strength, Is.Zero);
            Assert.That(flash.IsFlashing, Is.False);
        }

        [Test]
        public void A_second_Weapon_running_dry_starts_it_over_at_full_strength()
        {
            flash.Start();
            flash.Tick(0.375f);
            Assert.That(flash.Strength, Is.EqualTo(0.25f));

            flash.Start();

            Assert.That(flash.Strength, Is.EqualTo(1f));
        }

        [Test]
        public void A_flash_of_no_time_never_shows()
        {
            var off = new RunDryFlash(0f);

            off.Start();

            Assert.That(off.IsFlashing, Is.False);
            Assert.That(off.Strength, Is.Zero);
        }

        [Test]
        public void Its_time_and_its_steps_are_never_negative()
        {
            Assert.That(() => new RunDryFlash(-0.1f), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new RunDryFlash(float.NaN), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => flash.Tick(-0.1f), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}
