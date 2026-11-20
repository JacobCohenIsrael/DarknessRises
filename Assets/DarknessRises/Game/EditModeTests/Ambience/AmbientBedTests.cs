using System;
using DarknessRises.Game.Ambience;
using DarknessRises.Game.Critters;
using DarknessRises.Game.EditModeTests.Levels;
using DarknessRises.Game.Randomness;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Ambience
{
    public sealed class AmbientBedTests
    {
        private const float Frame = 1f / 64f;

        [TestCase(CritterMoment.Walking, true)]
        [TestCase(CritterMoment.AfterClear, true)]
        [TestCase(CritterMoment.WaveFight, true)]
        [TestCase(CritterMoment.BossFight, false)]
        [TestCase(CritterMoment.None, false)]
        public void A_bed_plays_in_every_moment_but_a_Boss_fight_and_none(CritterMoment moment, bool allowed)
        {
            Assert.That(AmbientBedSchedule.Allows(moment), Is.EqualTo(allowed));
        }

        [Test]
        public void The_first_bed_waits_for_the_first_delay()
        {
            var schedule = new AmbientBedSchedule(20f, 20f, 3f, new ScriptedRandom());

            for (var t = 0f; t < 3f - Frame; t += Frame)
            {
                Assert.That(schedule.Tick(Frame, CritterMoment.Walking, false), Is.False);
            }

            Assert.That(schedule.Tick(Frame, CritterMoment.Walking, false), Is.True);
        }

        [Test]
        public void The_gap_counts_from_when_a_bed_ends()
        {
            var random = new ScriptedRandom();
            random.Next(10f);
            var schedule = new AmbientBedSchedule(5f, 20f, 0f, random);
            Assert.That(schedule.Tick(Frame, CritterMoment.Walking, false), Is.True);
            Assert.That(schedule.GapLeft, Is.EqualTo(10f));

            // While it plays, the gap waits.
            for (var i = 0; i < 640; i++)
            {
                Assert.That(schedule.Tick(Frame, CritterMoment.Walking, true), Is.False);
            }

            Assert.That(schedule.GapLeft, Is.EqualTo(10f));
        }

        [Test]
        public void No_bed_starts_in_a_Boss_fight_but_one_comes_once_it_is_over()
        {
            var schedule = new AmbientBedSchedule(20f, 20f, 0f, new ScriptedRandom());

            Assert.That(schedule.Tick(Frame, CritterMoment.BossFight, false), Is.False);
            Assert.That(schedule.Tick(Frame, CritterMoment.Walking, false), Is.True);
        }

        [Test]
        public void Beds_come_now_and_then()
        {
            var schedule = new AmbientBedSchedule(20f, 45f, 8f, new SystemRandom(3));
            var count = 0;
            for (var t = 0f; t < 600f; t += Frame)
            {
                count += schedule.Tick(Frame, CritterMoment.Walking, false) ? 1 : 0;
            }

            // A bed every 20 to 45 s of silence (the beds' own length aside): 13 to 30 in 10 minutes.
            Assert.That(count, Is.InRange(13, 30));
        }

        [Test]
        public void A_slice_fits_in_its_clip()
        {
            var random = new SystemRandom(8);
            for (var i = 0; i < 500; i++)
            {
                var slice = AmbientSlice.Plan(24.4f, 6f, 12f, 2.5f, random);

                Assert.That(slice.Seconds, Is.InRange(6f, 12f));
                Assert.That(slice.StartSeconds, Is.GreaterThanOrEqualTo(0f));
                Assert.That(slice.StartSeconds + slice.Seconds, Is.LessThanOrEqualTo(24.4f + 1e-4f));
            }
        }

        [Test]
        public void A_clip_shorter_than_a_slice_plays_whole()
        {
            var slice = AmbientSlice.Plan(4f, 6f, 12f, 1f, new SystemRandom(1));

            Assert.That(slice.Seconds, Is.EqualTo(4f));
            Assert.That(slice.StartSeconds, Is.EqualTo(0f));
        }

        [Test]
        public void A_slice_fades_in_holds_and_fades_out()
        {
            var slice = new AmbientSlice(3f, 10f, 2f);

            Assert.That(slice.Volume(0f), Is.EqualTo(0f));
            Assert.That(slice.Volume(1f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(slice.Volume(5f), Is.EqualTo(1f));
            Assert.That(slice.Volume(9f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(slice.Volume(10f), Is.EqualTo(0f));
            Assert.That(slice.IsOver(9.9f), Is.False);
            Assert.That(slice.IsOver(10f), Is.True);
        }

        [Test]
        public void A_fade_longer_than_half_a_slice_is_cut_to_half()
        {
            var slice = new AmbientSlice(0f, 2f, 5f);

            Assert.That(slice.FadeSeconds, Is.EqualTo(1f));
            Assert.That(slice.Volume(1f), Is.EqualTo(1f));
        }

        [Test]
        public void A_bed_comes_from_out_in_the_dark_up_near_the_ceiling()
        {
            var random = new SystemRandom(6);
            var character = new Vector3(4f, 0.2f, -3f);
            for (var i = 0; i < 300; i++)
            {
                var place = AmbientBedPlace.Pick(character, 10f, 16f, 3.5f, random);
                var flat = place - character;
                flat.y = 0f;

                Assert.That(flat.magnitude, Is.InRange(10f - 1e-3f, 16f + 1e-3f));
                Assert.That(place.y, Is.EqualTo(3.7f).Within(1e-4f));
            }
        }

        [Test]
        public void Bad_settings_are_refused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AmbientBedSchedule(5f, 2f, 0f, new ScriptedRandom()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AmbientBedSchedule(0f, 1f, -1f, new ScriptedRandom()));
            Assert.Throws<ArgumentNullException>(() => new AmbientBedSchedule(0f, 1f, 0f, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => AmbientSlice.Plan(10f, 5f, 2f, 0f, new ScriptedRandom()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AmbientSlice(-1f, 1f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => AmbientBedPlace.Pick(Vector3.zero, 5f, 2f, 0f, new ScriptedRandom()));
        }

        [Test]
        public void Ticking_does_not_allocate()
        {
            var schedule = new AmbientBedSchedule(1f, 2f, 0f, new SystemRandom(2));
            var slice = new AmbientSlice(0f, 10f, 2f);

            AllocationAssert.DoesNotAllocate(() =>
            {
                for (var i = 0; i < 64; i++)
                {
                    schedule.Tick(Frame, CritterMoment.Walking, false);
                    _ = slice.Volume(i * Frame);
                }
            });
        }
    }
}
