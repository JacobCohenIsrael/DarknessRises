using System;
using DarknessRises.Game.Critters;
using DarknessRises.Game.EditModeTests.Levels;
using DarknessRises.Game.Phantoms;
using DarknessRises.Game.Randomness;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Phantoms
{
    public sealed class PhantomEyesScheduleTests
    {
        private const float Frame = 1f / 64f;
        private const float Clearance = 14f;
        private const float NoEnemy = float.PositiveInfinity;

        // Gaps of exactly gapSeconds, eyes every time unless voiceShare says otherwise.
        private static PhantomEyesSchedule Schedule(
            float gapSeconds = 1f,
            float firstDelay = 0f,
            float voiceShare = 0f,
            int maxPairs = 2,
            float retry = 0.25f,
            IRandom random = null) =>
            new(gapSeconds, gapSeconds, voiceShare, firstDelay, retry, Clearance, maxPairs, random ?? new ScriptedRandom());

        private static int Count(PhantomEyesSchedule schedule, float seconds, PhantomEvent kind, int pairsShowing = 0)
        {
            var count = 0;
            for (var t = 0f; t < seconds; t += Frame)
            {
                count += schedule.Tick(Frame, CritterMoment.Walking, NoEnemy, pairsShowing) == kind ? 1 : 0;
            }

            return count;
        }

        [TestCase(CritterMoment.Walking, true)]
        [TestCase(CritterMoment.AfterClear, true)]
        [TestCase(CritterMoment.WaveFight, false)]
        [TestCase(CritterMoment.BossFight, false)]
        [TestCase(CritterMoment.None, false)]
        public void They_may_show_only_in_a_quiet_moment(CritterMoment moment, bool may)
        {
            Assert.That(PhantomEyesSchedule.MayShow(moment, NoEnemy, Clearance), Is.EqualTo(may));
        }

        [Test]
        public void They_never_show_while_a_real_Enemy_is_near()
        {
            Assert.That(PhantomEyesSchedule.MayShow(CritterMoment.Walking, 13.9f, Clearance), Is.False);
            Assert.That(PhantomEyesSchedule.MayShow(CritterMoment.Walking, 14f, Clearance), Is.False);
            Assert.That(PhantomEyesSchedule.MayShow(CritterMoment.Walking, 14.1f, Clearance), Is.True);
        }

        [Test]
        public void Nothing_comes_in_a_fight_or_with_an_Enemy_near_and_that_time_does_not_count()
        {
            var schedule = Schedule(gapSeconds: 1f, firstDelay: 1f);

            for (var t = 0f; t < 30f; t += Frame)
            {
                Assert.That(schedule.Tick(Frame, CritterMoment.WaveFight, NoEnemy, 0), Is.EqualTo(PhantomEvent.None));
                Assert.That(schedule.Tick(Frame, CritterMoment.BossFight, NoEnemy, 0), Is.EqualTo(PhantomEvent.None));
                Assert.That(schedule.Tick(Frame, CritterMoment.Walking, 5f, 0), Is.EqualTo(PhantomEvent.None));
            }

            Assert.That(schedule.GapLeft, Is.EqualTo(1f));
        }

        [Test]
        public void Eyes_come_after_each_gap_of_safe_time()
        {
            var schedule = Schedule(gapSeconds: 10f, firstDelay: 5f);

            Assert.That(Count(schedule, 4.9f, PhantomEvent.Eyes), Is.Zero);
            Assert.That(Count(schedule, 0.2f, PhantomEvent.Eyes), Is.EqualTo(1));
            Assert.That(Count(schedule, 9.8f, PhantomEvent.Eyes), Is.Zero);
            Assert.That(Count(schedule, 0.2f, PhantomEvent.Eyes), Is.EqualTo(1));
        }

        [Test]
        public void Over_ten_minutes_of_walking_a_pair_comes_every_five_to_ten_seconds()
        {
            // The authored tuning (#102): 3-7 s gaps, a quarter of them lone voices, up to 3 pairs.
            var schedule = new PhantomEyesSchedule(3f, 7f, 0.25f, 4f, 1f, Clearance, 3, new SystemRandom(3));
            var eyes = 0;
            var voices = 0;
            for (var t = 0f; t < 600f; t += Frame)
            {
                var due = schedule.Tick(Frame, CritterMoment.Walking, NoEnemy, 0);
                eyes += due == PhantomEvent.Eyes ? 1 : 0;
                voices += due == PhantomEvent.Voice ? 1 : 0;
            }

            // 600 s: a pair every 5-10 s is 60-120, events every ~5 s about 120.
            Assert.That(eyes, Is.InRange(60, 120));
            Assert.That(voices, Is.InRange(15, 50));
        }

        [Test]
        public void An_authored_share_of_events_are_lone_voices()
        {
            var random = new ScriptedRandom();
            random.Next(0.1f, 1f, 0.9f, 1f);
            var schedule = Schedule(gapSeconds: 1f, voiceShare: 0.5f, random: random);

            Assert.That(schedule.Next, Is.EqualTo(PhantomEvent.Voice));
            Assert.That(schedule.Tick(Frame, CritterMoment.Walking, NoEnemy, 0), Is.EqualTo(PhantomEvent.Voice));
            Assert.That(schedule.Next, Is.EqualTo(PhantomEvent.Eyes));
        }

        [Test]
        public void Eyes_wait_while_the_most_pairs_are_out_but_a_voice_does_not()
        {
            var schedule = Schedule(maxPairs: 2);
            Assert.That(Count(schedule, 3f, PhantomEvent.Eyes, pairsShowing: 2), Is.Zero);
            Assert.That(schedule.Tick(Frame, CritterMoment.Walking, NoEnemy, 1), Is.EqualTo(PhantomEvent.Eyes));

            var voices = Schedule(voiceShare: 1f, maxPairs: 1, random: AlwaysLow());
            Assert.That(voices.Tick(Frame, CritterMoment.Walking, NoEnemy, 1), Is.EqualTo(PhantomEvent.Voice));
        }

        [Test]
        public void A_missed_event_tries_again_soon()
        {
            var schedule = Schedule(gapSeconds: 10f, retry: 0.5f);
            Assert.That(schedule.Tick(Frame, CritterMoment.Walking, NoEnemy, 0), Is.EqualTo(PhantomEvent.Eyes));

            schedule.Missed(PhantomEvent.Eyes);

            Assert.That(schedule.GapLeft, Is.EqualTo(0.5f));
            Assert.That(schedule.Next, Is.EqualTo(PhantomEvent.Eyes));
            Assert.That(Count(schedule, 0.6f, PhantomEvent.Eyes), Is.EqualTo(1));
        }

        [Test]
        public void Bad_settings_are_refused()
        {
            var random = new ScriptedRandom();
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomEyesSchedule(-1f, 1f, 0f, 0f, 0f, 0f, 1, random));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomEyesSchedule(2f, 1f, 0f, 0f, 0f, 0f, 1, random));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomEyesSchedule(0f, 1f, 1.5f, 0f, 0f, 0f, 1, random));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomEyesSchedule(0f, 1f, 0f, -1f, 0f, 0f, 1, random));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomEyesSchedule(0f, 1f, 0f, 0f, -1f, 0f, 1, random));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomEyesSchedule(0f, 1f, 0f, 0f, 0f, -1f, 1, random));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomEyesSchedule(0f, 1f, 0f, 0f, 0f, 0f, 0, random));
            Assert.Throws<ArgumentNullException>(() => new PhantomEyesSchedule(0f, 1f, 0f, 0f, 0f, 0f, 1, null));
        }

        [Test]
        public void A_spot_is_ahead_and_far_out_beyond_where_real_eyes_glint()
        {
            var spot = new PhantomEyesSpot(50f, 9f, 13f);
            var random = new SystemRandom(4);
            var character = new Vector3(2f, 0f, 1f);
            var forward = new Vector3(-1f, 0f, 0.5f);
            for (var i = 0; i < 500; i++)
            {
                var picked = spot.Pick(character, forward, random);
                var offset = picked - character;

                Assert.That(offset.magnitude, Is.InRange(9f - 1e-3f, 13f + 1e-3f));
                Assert.That(Vector3.Angle(forward, offset), Is.LessThanOrEqualTo(50f + 1e-3f));
                Assert.That(picked.y, Is.EqualTo(character.y));
                Assert.That(spot.IsFarOut(picked, character), Is.True);
            }
        }

        [Test]
        public void A_spot_nearer_or_further_than_eyes_appear_is_not_far_out()
        {
            var spot = new PhantomEyesSpot(120f, 8.5f, 12f);

            Assert.That(spot.IsFarOut(new Vector3(0f, 3f, 7f), Vector3.zero), Is.False);
            Assert.That(spot.IsFarOut(new Vector3(0f, 0f, 13f), Vector3.zero), Is.False);
            Assert.That(spot.IsFarOut(new Vector3(6f, 0f, 7f), Vector3.zero), Is.True);
        }

        [Test]
        public void Two_pairs_never_share_a_spot()
        {
            var others = new[] { new Vector3(0f, 0f, 10f), new Vector3(5f, 0f, 10f), new Vector3(100f, 0f, 0f) };

            Assert.That(PhantomEyesSpot.IsApart(new Vector3(1f, 0f, 10f), others, 2, 3f), Is.False);
            Assert.That(PhantomEyesSpot.IsApart(new Vector3(2.5f, 0f, 13f), others, 2, 3f), Is.True);

            // Only the first count are others.
            Assert.That(PhantomEyesSpot.IsApart(new Vector3(100f, 0f, 1f), others, 2, 3f), Is.True);
        }

        [Test]
        public void A_spot_the_walk_would_bring_the_character_up_to_is_refused()
        {
            var character = Vector3.zero;

            // 9 m straight ahead: 5 m of walking brings it to 4 m.
            Assert.That(PhantomEyesSpot.StaysAway(new Vector3(0f, 0f, 9f), character, Vector3.forward, 5f, 7.5f), Is.False);

            // 9 m off to the side: the walk passes it no nearer than 9 m.
            Assert.That(PhantomEyesSpot.StaysAway(new Vector3(9f, 0f, 2f), character, Vector3.forward, 5f, 7.5f), Is.True);

            // Behind: walking away only takes the character further.
            Assert.That(PhantomEyesSpot.StaysAway(new Vector3(0f, 0f, -8f), character, Vector3.forward, 5f, 7.5f), Is.True);

            // Ahead but beyond the walk: its nearest is at the walk's end, 10 m on.
            Assert.That(PhantomEyesSpot.StaysAway(new Vector3(0f, 0f, 15f), character, Vector3.forward, 5f, 7.5f), Is.True);
        }

        [Test]
        public void A_bad_spot_shape_is_refused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomEyesSpot(190f, 9f, 13f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomEyesSpot(50f, 13f, 9f));
        }

        [Test]
        public void Ticking_does_not_allocate()
        {
            var schedule = new PhantomEyesSchedule(1f, 3f, 0.25f, 0f, 1f, Clearance, 2, new SystemRandom(1));

            AllocationAssert.DoesNotAllocate(() =>
            {
                for (var i = 0; i < 640; i++)
                {
                    schedule.Tick(Frame, CritterMoment.Walking, NoEnemy, i % 3);
                }
            });
        }

        private static ScriptedRandom AlwaysLow()
        {
            var random = new ScriptedRandom();
            for (var i = 0; i < 1000; i++)
            {
                random.Next(0f);
            }

            return random;
        }
    }
}
