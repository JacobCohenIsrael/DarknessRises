using System;
using DarknessRises.Game.Audio;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Audio
{
    public sealed class SoundThrottleTests
    {
        private const float Interval = 0.4f;

        [Test]
        public void The_first_play_goes_through()
        {
            Assert.That(new SoundThrottle(Interval).TryPlay(), Is.True);
        }

        [Test]
        public void A_play_sooner_than_the_interval_is_held_back()
        {
            var throttle = new SoundThrottle(Interval);
            throttle.TryPlay();
            throttle.Tick(Interval * 0.9f);

            Assert.That(throttle.TryPlay(), Is.False);
        }

        [Test]
        public void A_play_once_the_interval_has_passed_goes_through()
        {
            var throttle = new SoundThrottle(Interval);
            throttle.TryPlay();
            throttle.Tick(Interval);

            Assert.That(throttle.TryPlay(), Is.True);
        }

        [Test]
        public void A_held_back_play_does_not_start_the_interval_again()
        {
            // Shots every 0.1 s: the sound plays on the first and then every fourth, not never again.
            var throttle = new SoundThrottle(Interval);
            var plays = 0;
            for (var shot = 0; shot < 12; shot++)
            {
                if (throttle.TryPlay())
                {
                    plays++;
                }

                throttle.Tick(0.1f + 1e-4f);
            }

            Assert.That(plays, Is.EqualTo(3));
        }

        [Test]
        public void No_interval_lets_every_play_through()
        {
            var throttle = new SoundThrottle(0f);
            Assert.That(throttle.TryPlay(), Is.True);
            Assert.That(throttle.TryPlay(), Is.True);
        }

        [Test]
        public void Rejects_a_negative_interval_or_time()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SoundThrottle(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SoundThrottle(Interval).Tick(-1f));
        }
    }
}
