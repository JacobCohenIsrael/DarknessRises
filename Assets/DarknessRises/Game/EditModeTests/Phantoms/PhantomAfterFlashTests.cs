using System;
using DarknessRises.Game.Phantoms;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Phantoms
{
    public sealed class PhantomAfterFlashTests
    {
        private const float Frame = 1f / 64f;

        // Open within 0.25 s of a flash fading; wait 4 s at most.
        private static PhantomAfterFlash Timing() => new(0.25f, 4f);

        private static bool Run(PhantomAfterFlash timing, float seconds, bool lit)
        {
            for (var t = 0f; t < seconds; t += Frame)
            {
                if (timing.Tick(Frame, lit))
                {
                    return true;
                }
            }

            return false;
        }

        [Test]
        public void Every_pair_waiting_opens_as_the_same_flash_fades_one_a_frame()
        {
            var timing = Timing();
            timing.Wait();
            timing.Wait();
            timing.Wait();
            Assert.That(timing.Waiting, Is.EqualTo(3));
            Assert.That(Run(timing, 0.4f, true), Is.False);

            Assert.That(timing.Tick(Frame, false), Is.True);
            Assert.That(timing.Tick(Frame, false), Is.True);
            Assert.That(timing.Tick(Frame, false), Is.True);
            Assert.That(timing.IsWaiting, Is.False);
            Assert.That(timing.Tick(Frame, false), Is.False);
        }

        [Test]
        public void Cancelling_calls_off_every_waiting_pair()
        {
            var timing = Timing();
            timing.Wait();
            timing.Wait();
            timing.Cancel();

            Assert.That(timing.Waiting, Is.Zero);
            Assert.That(Run(timing, 0.4f, true), Is.False);
            Assert.That(Run(timing, 5f, false), Is.False);
        }

        [Test]
        public void Due_eyes_wait_through_the_dark_and_the_flash_then_open_as_it_fades()
        {
            var timing = Timing();
            Run(timing, 2f, false);
            timing.Wait();

            Assert.That(Run(timing, 1f, false), Is.False);
            Assert.That(Run(timing, 0.4f, true), Is.False);
            Assert.That(timing.Tick(Frame, false), Is.True);
            Assert.That(timing.IsWaiting, Is.False);
        }

        [Test]
        public void Without_a_flash_they_open_after_the_longest_wait()
        {
            var timing = Timing();
            timing.Wait();

            Assert.That(Run(timing, 4f - Frame, false), Is.False);
            Assert.That(Run(timing, 2f * Frame, false), Is.True);
        }

        [Test]
        public void They_never_open_into_a_lit_Room_however_long_they_waited()
        {
            var timing = Timing();
            timing.Wait();

            Assert.That(Run(timing, 10f, true), Is.False);
            Assert.That(timing.Tick(Frame, false), Is.True);
        }

        [Test]
        public void Nothing_opens_unless_a_pair_is_waiting()
        {
            var timing = Timing();

            Assert.That(Run(timing, 0.5f, true), Is.False);
            Assert.That(Run(timing, 10f, false), Is.False);
        }

        [Test]
        public void A_cancelled_wait_opens_nothing()
        {
            var timing = Timing();
            timing.Wait();
            timing.Cancel();

            Assert.That(Run(timing, 0.5f, true), Is.False);
            Assert.That(Run(timing, 10f, false), Is.False);
        }

        [Test]
        public void Bad_settings_are_refused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomAfterFlash(-1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomAfterFlash(1f, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Timing().Tick(-1f, false));
        }

        [Test]
        public void Ticking_does_not_allocate()
        {
            var timing = Timing();

            AllocationAssert.DoesNotAllocate(() =>
            {
                for (var i = 0; i < 640; i++)
                {
                    if (i % 50 == 0)
                    {
                        timing.Wait();
                    }

                    timing.Tick(Frame, i % 160 < 25);
                }
            });
        }
    }
}
