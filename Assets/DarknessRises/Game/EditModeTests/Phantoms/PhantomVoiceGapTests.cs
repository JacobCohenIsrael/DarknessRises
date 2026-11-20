using System;
using DarknessRises.Game.Phantoms;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Phantoms
{
    public sealed class PhantomVoiceGapTests
    {
        private const float Frame = 1f / 64f;

        private static void Run(PhantomVoiceGap gap, float seconds)
        {
            for (var t = 0f; t < seconds; t += Frame)
            {
                gap.Tick(Frame);
            }
        }

        [Test]
        public void The_first_voice_may_start_at_once()
        {
            Assert.That(new PhantomVoiceGap(4f).IsOpen, Is.True);
        }

        [Test]
        public void After_a_voice_the_next_waits_the_authored_gap()
        {
            var gap = new PhantomVoiceGap(4f);
            gap.Started();

            Assert.That(gap.IsOpen, Is.False);
            Run(gap, 3.9f);
            Assert.That(gap.IsOpen, Is.False);
            Run(gap, 0.15f);
            Assert.That(gap.IsOpen, Is.True);
        }

        [Test]
        public void Each_voice_starts_the_gap_again()
        {
            var gap = new PhantomVoiceGap(2f);
            gap.Started();
            Run(gap, 2.1f);
            gap.Started();

            Assert.That(gap.IsOpen, Is.False);
            Assert.That(gap.SinceLastSeconds, Is.EqualTo(0f));
        }

        [Test]
        public void A_zero_gap_never_holds_a_voice_back()
        {
            var gap = new PhantomVoiceGap(0f);
            gap.Started();

            Assert.That(gap.IsOpen, Is.True);
        }

        [Test]
        public void A_negative_gap_or_time_is_refused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomVoiceGap(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomVoiceGap(1f).Tick(-Frame));
        }

        [Test]
        public void Ticking_and_starting_do_not_allocate()
        {
            var gap = new PhantomVoiceGap(4f);
            AllocationAssert.DoesNotAllocate(() =>
            {
                for (var i = 0; i < 640; i++)
                {
                    if (i % 300 == 0)
                    {
                        gap.Started();
                    }

                    gap.Tick(Frame);
                    _ = gap.IsOpen;
                }
            });
        }
    }
}
