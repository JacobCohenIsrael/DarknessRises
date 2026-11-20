using System;
using System.Collections.Generic;
using DarknessRises.Game.Hud;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Hud
{
    public sealed class CountUpTests
    {
        private List<int> shown;

        private CountUp NewCount()
        {
            shown = new List<int>();
            var count = new CountUp();
            count.ValueChanged += shown.Add;
            return count;
        }

        [Test]
        public void It_starts_at_zero_and_isnt_counting()
        {
            var count = NewCount();

            count.Tick(1f);

            Assert.That(count.Value, Is.Zero);
            Assert.That(count.IsCounting, Is.False);
            Assert.That(shown, Is.Empty);
        }

        [Test]
        public void Set_shows_the_number_at_once()
        {
            var count = NewCount();

            count.Set(250);

            Assert.That(count.Value, Is.EqualTo(250));
            Assert.That(count.IsCounting, Is.False);
            Assert.That(shown, Is.EqualTo(new[] { 250 }));
        }

        [Test]
        public void It_counts_up_to_the_target_over_its_time()
        {
            var count = NewCount();
            count.CountTo(100, 1f);

            Assert.That(count.IsCounting, Is.True);
            count.Tick(0.5f);
            Assert.That(count.Value, Is.GreaterThan(50).And.LessThan(100), "It eases out: more than half the way in half the time.");

            count.Tick(0.5f);
            Assert.That(count.Value, Is.EqualTo(100));
            Assert.That(count.IsCounting, Is.False);
        }

        [Test]
        public void It_never_counts_past_the_target_or_back()
        {
            var count = NewCount();
            count.CountTo(37, 1f);

            for (var i = 0; i < 200; i++)
            {
                count.Tick(0.01f);
            }

            Assert.That(count.Value, Is.EqualTo(37));
            Assert.That(shown, Is.Ordered.Ascending);
            Assert.That(shown, Is.Unique);
            Assert.That(shown[shown.Count - 1], Is.EqualTo(37));
        }

        [Test]
        public void It_counts_on_from_where_it_stands()
        {
            var count = NewCount();
            count.Set(300);

            count.CountTo(450, 1f);
            count.Tick(0.25f);

            Assert.That(count.Value, Is.GreaterThan(300).And.LessThan(450));
            count.Tick(1f);
            Assert.That(count.Value, Is.EqualTo(450));
        }

        [Test]
        public void With_no_time_it_shows_the_target_at_once()
        {
            var count = NewCount();

            count.CountTo(80, 0f);

            Assert.That(count.Value, Is.EqualTo(80));
            Assert.That(count.IsCounting, Is.False);
        }

        [Test]
        public void It_tells_each_number_once()
        {
            var count = NewCount();
            count.CountTo(3, 1f);

            count.Tick(0f);
            count.Tick(0f);
            count.Tick(2f);
            count.Tick(2f);

            Assert.That(shown, Is.EqualTo(new[] { 3 }));
        }

        [Test]
        public void Time_only_moves_forward()
        {
            var count = NewCount();

            Assert.That(() => count.Tick(-0.1f), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => count.CountTo(10, -1f), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Counting_allocates_nothing()
        {
            var count = new CountUp();
            var last = 0;
            count.ValueChanged += value => last = value;

            AllocationAssert.DoesNotAllocate(() =>
            {
                count.Set(0);
                count.CountTo(1000, 1f);
                for (var i = 0; i < 120; i++)
                {
                    count.Tick(0.01f);
                }
            });

            Assert.That(last, Is.EqualTo(1000));
        }
    }
}
