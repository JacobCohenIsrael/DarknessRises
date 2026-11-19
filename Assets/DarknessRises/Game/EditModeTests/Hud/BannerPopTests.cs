using System;
using DarknessRises.Game.Hud;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Hud
{
    public sealed class BannerPopTests
    {
        private const float Tolerance = 1e-4f;

        // Pops in over 0.2 s from twice its size, fades out over 0.5 s.
        private static BannerPop Pop() => new(0.2f, 2f, 0.5f);

        [Test]
        public void Hidden_until_shown()
        {
            var pop = Pop();

            pop.Tick(1f);

            Assert.That(pop.Alpha, Is.Zero);
            Assert.That(pop.IsVisible, Is.False);
        }

        [Test]
        public void Pops_in_from_larger_than_life_while_it_fades_in()
        {
            var pop = Pop();

            pop.Show();
            Assert.That(pop.Alpha, Is.Zero);
            Assert.That(pop.Scale, Is.EqualTo(2f));

            pop.Tick(0.1f);
            Assert.That(pop.Alpha, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(pop.Scale, Is.GreaterThan(1f).And.LessThan(1.5f), "It settles fast at first.");

            pop.Tick(0.1f);
            Assert.That(pop.Alpha, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(pop.Scale, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Stays_until_it_is_hidden()
        {
            var pop = Pop();
            pop.Show();

            pop.Tick(60f);

            Assert.That(pop.Alpha, Is.EqualTo(1f));
            Assert.That(pop.Scale, Is.EqualTo(1f));
        }

        [Test]
        public void Fades_out_once_hidden()
        {
            var pop = Pop();
            pop.Show();
            pop.Tick(1f);

            pop.Hide();
            pop.Tick(0.25f);
            Assert.That(pop.Alpha, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(pop.Scale, Is.EqualTo(1f), "It fades at its own size.");

            pop.Tick(0.25f);
            Assert.That(pop.Alpha, Is.Zero);
            Assert.That(pop.IsVisible, Is.False);
        }

        [Test]
        public void Lingers_before_it_fades_when_asked_to()
        {
            var pop = Pop();
            pop.Show();
            pop.Tick(1f);

            pop.Hide(lingerSeconds: 1f);
            pop.Tick(0.5f);
            Assert.That(pop.Alpha, Is.EqualTo(1f));

            // The linger runs out half a second in; the other quarter second fades.
            pop.Tick(0.75f);
            Assert.That(pop.Alpha, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void Showing_again_pops_in_afresh_even_while_fading()
        {
            var pop = Pop();
            pop.Show();
            pop.Tick(1f);
            pop.Hide();
            pop.Tick(0.25f);

            pop.Show();
            Assert.That(pop.Scale, Is.EqualTo(2f));

            pop.Tick(10f);
            Assert.That(pop.Alpha, Is.EqualTo(1f), "The earlier hide no longer counts.");
        }

        [Test]
        public void Hiding_a_hidden_banner_does_nothing()
        {
            var pop = Pop();

            pop.Hide(1f);
            pop.Tick(2f);

            Assert.That(pop.Alpha, Is.Zero);
        }

        [Test]
        public void No_pop_and_no_fade_snap()
        {
            var pop = new BannerPop(0f, 1f, 0f);

            pop.Show();
            Assert.That(pop.Alpha, Is.EqualTo(1f));
            Assert.That(pop.Scale, Is.EqualTo(1f));

            pop.Hide();
            pop.Tick(0f);
            Assert.That(pop.Alpha, Is.Zero);
        }

        [Test]
        public void Ticking_does_not_allocate()
        {
            var pop = Pop();
            pop.Show();

            AllocationAssert.DoesNotAllocate(() => pop.Tick(0.001f));
        }

        [Test]
        public void Rejects_timings_that_make_no_sense()
        {
            Assert.That(() => new BannerPop(-1f, 1f, 0f), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new BannerPop(0f, 0.5f, 0f), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new BannerPop(0f, 1f, -1f), Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
