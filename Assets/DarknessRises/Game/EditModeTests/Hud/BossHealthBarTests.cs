using System;
using DarknessRises.Game.Hud;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Hud
{
    public sealed class BossHealthBarTests
    {
        private const float Hold = 0.5f;
        private const float DrainPerSecond = 0.5f;
        private const float Slide = 0.4f;
        private const float Tolerance = 0.0001f;

        private BossHealthBar bar;

        [SetUp]
        public void SetUp()
        {
            bar = new BossHealthBar(Hold, DrainPerSecond, Slide);
        }

        [Test]
        public void It_starts_out_of_sight_and_slides_in_over_the_slide_time()
        {
            Assert.That(bar.IsOnScreen, Is.False);

            bar.Show(1f);
            bar.Tick(Slide / 2f, 1f);
            Assert.That(bar.SlideIn, Is.EqualTo(0.5f).Within(Tolerance));

            bar.Tick(Slide, 1f);
            Assert.That(bar.SlideIn, Is.EqualTo(1f));
        }

        [Test]
        public void It_slides_out_when_hidden()
        {
            bar.Show(1f);
            bar.Tick(1f, 1f);

            bar.Hide();
            bar.Tick(Slide / 4f, 1f);
            Assert.That(bar.SlideIn, Is.EqualTo(0.75f).Within(Tolerance));
            Assert.That(bar.IsOnScreen, Is.True);

            bar.Tick(Slide, 1f);
            Assert.That(bar.IsOnScreen, Is.False);
        }

        [Test]
        public void After_a_hit_the_trail_holds_then_drains_to_the_fill()
        {
            bar.Show(1f);
            bar.Tick(1f, 1f);

            bar.Hit();
            bar.Tick(Hold - 0.1f, 0.8f);
            Assert.That(bar.Trail, Is.EqualTo(1f), "Still holding.");

            bar.Tick(0.2f, 0.8f);
            Assert.That(bar.Trail, Is.EqualTo(1f - DrainPerSecond * 0.1f).Within(Tolerance), "Drains only for the time past the hold.");

            bar.Tick(1f, 0.8f);
            Assert.That(bar.Trail, Is.EqualTo(0.8f), "Never drains below the fill.");
        }

        [Test]
        public void Another_hit_while_draining_holds_the_trail_again()
        {
            bar.Show(1f);
            bar.Hit();
            bar.Tick(Hold + 0.2f, 0.5f);
            var trail = bar.Trail;

            bar.Hit();
            bar.Tick(Hold - 0.1f, 0.4f);

            Assert.That(bar.Trail, Is.EqualTo(trail), "The trail keeps the hit points lost by both hits.");
        }

        [Test]
        public void The_trail_never_sits_below_the_fill()
        {
            bar.Show(0.5f);
            bar.Tick(0.1f, 0.7f);

            Assert.That(bar.Trail, Is.EqualTo(0.7f));
        }

        [Test]
        public void A_new_fight_starts_with_no_trail()
        {
            bar.Show(1f);
            bar.Hit();
            bar.Tick(0.1f, 0.2f);

            bar.Show(1f);
            bar.Tick(0f, 1f);

            Assert.That(bar.Trail, Is.EqualTo(1f));
        }

        [Test]
        public void Time_cannot_run_backwards_and_settings_must_make_sense()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => bar.Tick(-0.1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BossHealthBar(-1f, 1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BossHealthBar(1f, 0f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BossHealthBar(1f, 1f, -1f));
        }
    }
}
