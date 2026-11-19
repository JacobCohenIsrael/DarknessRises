using System;
using System.Collections.Generic;
using DarknessRises.Game.Continues;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Continues
{
    public sealed class ContinueCountdownTests
    {
        private List<int> counts;
        private int expirations;

        // From 3, one second a number.
        private ContinueCountdown NewCountdown(int from = 3, float secondsPerCount = 1f)
        {
            counts = new List<int>();
            expirations = 0;
            var countdown = new ContinueCountdown(from, secondsPerCount);
            countdown.CountChanged += counts.Add;
            countdown.Expired += () => expirations++;
            return countdown;
        }

        [Test]
        public void Nothing_counts_until_it_starts()
        {
            var countdown = NewCountdown();

            countdown.Tick(100f);

            Assert.That(countdown.IsRunning, Is.False);
            Assert.That(countdown.IsExpired, Is.False);
            Assert.That(counts, Is.Empty);
        }

        [Test]
        public void Starting_shows_the_first_number()
        {
            var countdown = NewCountdown();

            countdown.Start();

            Assert.That(countdown.Count, Is.EqualTo(3));
            Assert.That(countdown.IsRunning, Is.True);
            Assert.That(counts, Is.EqualTo(new[] { 3 }));
        }

        [Test]
        public void It_counts_down_one_number_at_a_time_to_zero()
        {
            var countdown = NewCountdown();
            countdown.Start();

            countdown.Tick(0.9f);
            Assert.That(countdown.Count, Is.EqualTo(3));

            countdown.Tick(0.2f);
            Assert.That(countdown.Count, Is.EqualTo(2));

            countdown.Tick(1f);
            countdown.Tick(1f);
            Assert.That(countdown.Count, Is.Zero);
            Assert.That(countdown.IsExpired, Is.False, "Zero gets its turn too.");
            Assert.That(counts, Is.EqualTo(new[] { 3, 2, 1, 0 }));
        }

        [Test]
        public void It_expires_once_when_zero_has_had_its_turn()
        {
            var countdown = NewCountdown();
            countdown.Start();

            countdown.Tick(3.9f);
            Assert.That(expirations, Is.Zero);

            countdown.Tick(0.2f);
            countdown.Tick(10f);

            Assert.That(countdown.IsExpired, Is.True);
            Assert.That(countdown.IsRunning, Is.False);
            Assert.That(expirations, Is.EqualTo(1));
        }

        [Test]
        public void A_long_step_passes_every_number_on_the_way()
        {
            var countdown = NewCountdown();
            countdown.Start();

            countdown.Tick(2.5f);

            Assert.That(counts, Is.EqualTo(new[] { 3, 2, 1 }));
        }

        [Test]
        public void Stopping_holds_it_where_it_is()
        {
            var countdown = NewCountdown();
            countdown.Start();
            countdown.Tick(1.5f);

            countdown.Stop();
            countdown.Tick(60f);

            Assert.That(countdown.Count, Is.EqualTo(2));
            Assert.That(countdown.IsExpired, Is.False);
            Assert.That(expirations, Is.Zero);
        }

        [Test]
        public void Starting_again_counts_afresh_even_after_it_expired()
        {
            var countdown = NewCountdown();
            countdown.Start();
            countdown.Tick(10f);

            countdown.Start();

            Assert.That(countdown.Count, Is.EqualTo(3));
            Assert.That(countdown.IsExpired, Is.False);
            Assert.That(countdown.IsRunning, Is.True);
        }

        [Test]
        public void An_authored_ten_counts_ten_to_zero_in_eleven_seconds()
        {
            var countdown = NewCountdown(from: 10);
            countdown.Start();

            countdown.Tick(10.5f);
            Assert.That(countdown.Count, Is.Zero);
            Assert.That(countdown.IsExpired, Is.False);

            countdown.Tick(0.6f);
            Assert.That(countdown.IsExpired, Is.True);
            Assert.That(counts, Has.Count.EqualTo(11));
        }

        [Test]
        public void Ticking_does_not_allocate()
        {
            var countdown = new ContinueCountdown(10, 1000f);
            countdown.Start();

            AllocationAssert.DoesNotAllocate(() => countdown.Tick(0.001f));
        }

        [Test]
        public void Rejects_a_countdown_that_cant_count()
        {
            Assert.That(() => new ContinueCountdown(-1, 1f), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new ContinueCountdown(10, 0f), Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
