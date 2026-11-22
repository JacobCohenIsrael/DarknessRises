using System;
using DarknessRises.Game.Skills;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Skills
{
    public sealed class LureGlowTests
    {
        private const float Fade = 0.6f;
        private const float Pulse = 1f;
        private const float Depth = 0.4f;
        private const float Frame = 1f / 60f;
        private const float Tolerance = 1e-4f;

        [Test]
        public void It_is_dark_until_the_Lure_is_thrown()
        {
            var glow = new LureGlow(Fade, Pulse, Depth);
            glow.Tick(1f);

            Assert.That(glow.IsShining, Is.False);
            Assert.That(glow.Brightness, Is.Zero);
        }

        [Test]
        public void A_thrown_Lure_shines_at_full_strength_at_once()
        {
            var glow = new LureGlow(Fade, Pulse, Depth);
            glow.Light();

            Assert.That(glow.IsShining, Is.True);
            Assert.That(glow.IsFading, Is.False);
            Assert.That(glow.Brightness, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void While_out_it_pulses_slowly_between_full_and_the_authored_dip()
        {
            var glow = new LureGlow(Fade, Pulse, Depth);
            glow.Light();

            glow.Tick(Pulse / 2f);
            Assert.That(glow.Brightness, Is.EqualTo(1f - Depth).Within(Tolerance), "halfway through a pulse");

            glow.Tick(Pulse / 2f);
            Assert.That(glow.Brightness, Is.EqualTo(1f).Within(Tolerance), "a whole pulse later");

            for (var i = 0; i < 600; i++)
            {
                glow.Tick(Frame);
                Assert.That(glow.Brightness, Is.InRange(1f - Depth - Tolerance, 1f + Tolerance));
            }
        }

        [Test]
        public void With_no_pulse_it_shines_steadily()
        {
            var glow = new LureGlow(Fade, pulseSeconds: 0f, Depth);
            glow.Light();
            glow.Tick(0.37f);

            Assert.That(glow.Brightness, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void When_the_Lure_ends_it_fades_out_over_the_authored_time_with_no_pop()
        {
            var glow = new LureGlow(Fade, pulseSeconds: 0f, pulseDepth: 0f);
            glow.Light();
            glow.FadeOut();

            Assert.That(glow.Brightness, Is.EqualTo(1f).Within(Tolerance), "no pop the moment it ends");
            Assert.That(glow.IsFading, Is.True);

            glow.Tick(Fade / 2f);
            Assert.That(glow.Brightness, Is.EqualTo(0.5f).Within(Tolerance));

            var previous = glow.Brightness;
            while (glow.IsShining)
            {
                glow.Tick(Frame);
                Assert.That(glow.Brightness, Is.LessThanOrEqualTo(previous));
                previous = glow.Brightness;
            }

            Assert.That(glow.Brightness, Is.Zero);
            Assert.That(glow.IsFading, Is.False);
        }

        [Test]
        public void A_fade_takes_no_longer_than_its_authored_time()
        {
            var glow = new LureGlow(Fade, Pulse, Depth);
            glow.Light();
            glow.FadeOut();
            glow.Tick(Fade + Frame);

            Assert.That(glow.IsShining, Is.False);
        }

        [Test]
        public void With_no_fade_it_goes_dark_as_the_Lure_ends()
        {
            var glow = new LureGlow(fadeOutSeconds: 0f, Pulse, Depth);
            glow.Light();
            glow.FadeOut();

            Assert.That(glow.IsShining, Is.False);
            Assert.That(glow.Brightness, Is.Zero);
        }

        [Test]
        public void Throwing_again_while_it_fades_lights_it_fully_again()
        {
            var glow = new LureGlow(Fade, pulseSeconds: 0f, pulseDepth: 0f);
            glow.Light();
            glow.FadeOut();
            glow.Tick(Fade / 2f);
            glow.Light();
            glow.Tick(Fade);

            Assert.That(glow.Brightness, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(glow.IsFading, Is.False);
        }

        [Test]
        public void It_rejects_what_no_glow_can_be()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new LureGlow(-1f, Pulse, Depth));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new LureGlow(Fade, -1f, Depth));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new LureGlow(Fade, Pulse, -0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new LureGlow(Fade, Pulse, 1.1f));
        }

        [Test]
        public void Lighting_pulsing_and_fading_do_not_allocate()
        {
            var glow = new LureGlow(Fade, Pulse, Depth);
            AllocationAssert.DoesNotAllocate(() =>
            {
                for (var throwIndex = 0; throwIndex < 3; throwIndex++)
                {
                    glow.Light();
                    for (var i = 0; i < 300; i++)
                    {
                        glow.Tick(Frame);
                        _ = glow.Brightness;
                    }

                    glow.FadeOut();
                    while (glow.IsShining)
                    {
                        glow.Tick(Frame);
                        _ = glow.Brightness;
                    }
                }
            });
        }
    }
}
