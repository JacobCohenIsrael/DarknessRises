using System;
using System.Collections.Generic;
using DarknessRises.Game.Enemies.Tells;
using DarknessRises.Game.Randomness;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Enemies.Tells
{
    public sealed class TellSoundScheduleTests
    {
        [Test]
        public void Nothing_is_due_before_it_starts()
        {
            var schedule = new TellSoundSchedule(1f, 2f, new ScriptedRandom());

            Assert.That(schedule.Tick(100f), Is.False);
        }

        [Test]
        public void First_sound_comes_one_random_interval_after_spawning()
        {
            var random = new ScriptedRandom(1.5f);
            var schedule = new TellSoundSchedule(1f, 2f, random);

            schedule.Start();

            Assert.That(random.LastRange, Is.EqualTo((1f, 2f)));
            Assert.That(schedule.Tick(1.25f), Is.False);
            Assert.That(schedule.Tick(0.25f), Is.True);
        }

        [Test]
        public void Each_sound_schedules_the_next_one_a_fresh_interval_later()
        {
            var schedule = new TellSoundSchedule(1f, 4f, new ScriptedRandom(1f, 3f, 2f));
            schedule.Start();

            Assert.That(schedule.Tick(1f), Is.True);
            Assert.That(schedule.Tick(2.5f), Is.False);
            Assert.That(schedule.Tick(0.5f), Is.True);
            Assert.That(schedule.Tick(1.5f), Is.False);
            Assert.That(schedule.Tick(0.5f), Is.True);
        }

        [Test]
        public void A_long_frame_makes_one_sound_not_a_burst()
        {
            var schedule = new TellSoundSchedule(1f, 1f, new ScriptedRandom(1f, 1f, 1f));
            schedule.Start();

            Assert.That(schedule.Tick(10f), Is.True);
            Assert.That(schedule.Tick(0f), Is.False);
        }

        [Test]
        public void A_stopped_schedule_stays_quiet_until_started_again()
        {
            var schedule = new TellSoundSchedule(1f, 1f, new ScriptedRandom(1f, 1f));
            schedule.Start();
            schedule.Stop();

            Assert.That(schedule.IsRunning, Is.False);
            Assert.That(schedule.Tick(5f), Is.False);

            schedule.Start();
            Assert.That(schedule.Tick(1f), Is.True);
        }

        [TestCase(0f, 0)]
        [TestCase(1.99f, 1)]
        [TestCase(3f, 2)]
        public void Picks_an_index_within_the_choices(float roll, int expected)
        {
            var schedule = new TellSoundSchedule(1f, 1f, new ScriptedRandom(roll));

            Assert.That(schedule.PickIndex(3), Is.EqualTo(expected));
        }

        [Test]
        public void Invalid_settings_are_rejected()
        {
            var random = new ScriptedRandom();
            Assert.Throws<ArgumentOutOfRangeException>(() => new TellSoundSchedule(-1f, 1f, random));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TellSoundSchedule(2f, 1f, random));
            Assert.Throws<ArgumentNullException>(() => new TellSoundSchedule(1f, 2f, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TellSoundSchedule(1f, 2f, random).PickIndex(0));
        }

        [Test]
        public void Ticking_does_not_allocate()
        {
            var schedule = new TellSoundSchedule(0.5f, 1f, new SystemRandom(7));
            schedule.Start();

            AllocationAssert.DoesNotAllocate(() =>
            {
                for (var i = 0; i < 1000; i++)
                {
                    if (schedule.Tick(1f / 30f))
                    {
                        schedule.PickIndex(2);
                    }
                }
            });
        }

        private sealed class ScriptedRandom : IRandom
        {
            private readonly Queue<float> values;

            public ScriptedRandom(params float[] next)
            {
                values = new Queue<float>(next);
            }

            public (float, float) LastRange { get; private set; }

            public float Range(float min, float max)
            {
                LastRange = (min, max);
                return values.Count > 0 ? values.Dequeue() : max;
            }
        }
    }
}
