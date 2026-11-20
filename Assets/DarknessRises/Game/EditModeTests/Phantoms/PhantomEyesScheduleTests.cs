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

        private static ScriptedRandom AlwaysNow()
        {
            var random = new ScriptedRandom();
            for (var i = 0; i < 100000; i++)
            {
                random.Next(0f);
            }

            return random;
        }

        private static PhantomEyesSchedule Schedule(IRandom random, float cooldown = 0f, float firstDelay = 0f) =>
            new(1f, cooldown, firstDelay, Clearance, random);

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
        public void However_lucky_none_come_in_a_fight_or_with_an_Enemy_near()
        {
            var schedule = Schedule(AlwaysNow());

            for (var t = 0f; t < 30f; t += Frame)
            {
                Assert.That(schedule.Tick(Frame, CritterMoment.WaveFight, NoEnemy, false), Is.False);
                Assert.That(schedule.Tick(Frame, CritterMoment.BossFight, NoEnemy, false), Is.False);
                Assert.That(schedule.Tick(Frame, CritterMoment.Walking, 5f, false), Is.False);
            }
        }

        [Test]
        public void A_lucky_roll_in_a_quiet_moment_brings_them()
        {
            Assert.That(Schedule(AlwaysNow()).Tick(Frame, CritterMoment.Walking, NoEnemy, false), Is.True);
            Assert.That(Schedule(AlwaysNow()).Tick(Frame, CritterMoment.AfterClear, 20f, false), Is.True);
        }

        [Test]
        public void An_unlucky_roll_brings_none()
        {
            var schedule = Schedule(new ScriptedRandom());

            Assert.That(schedule.Tick(Frame, CritterMoment.Walking, NoEnemy, false), Is.False);
        }

        [Test]
        public void None_come_while_a_pair_is_still_showing()
        {
            Assert.That(Schedule(AlwaysNow()).Tick(Frame, CritterMoment.Walking, NoEnemy, true), Is.False);
        }

        [Test]
        public void They_are_rare_the_cooldown_spaces_them_out()
        {
            var schedule = Schedule(AlwaysNow(), cooldown: 45f);
            var count = 0;
            for (var t = 0f; t < 100f; t += Frame)
            {
                count += schedule.Tick(Frame, CritterMoment.Walking, NoEnemy, false) ? 1 : 0;
            }

            Assert.That(count, Is.EqualTo(3));
        }

        [Test]
        public void None_come_before_the_first_delay()
        {
            var schedule = Schedule(AlwaysNow(), firstDelay: 2f);

            for (var t = 0f; t < 2f; t += Frame)
            {
                Assert.That(schedule.Tick(Frame, CritterMoment.Walking, NoEnemy, false), Is.False);
            }

            Assert.That(schedule.Tick(Frame, CritterMoment.Walking, NoEnemy, false), Is.True);
        }

        [Test]
        public void Over_time_they_come_at_the_authored_rate()
        {
            var schedule = new PhantomEyesSchedule(0.1f, 0f, 0f, Clearance, new SystemRandom(9));
            var count = 0;
            for (var t = 0f; t < 600f; t += Frame)
            {
                count += schedule.Tick(Frame, CritterMoment.Walking, NoEnemy, false) ? 1 : 0;
            }

            Assert.That(count, Is.InRange(40, 90));
        }

        [Test]
        public void Bad_settings_are_refused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomEyesSchedule(-1f, 0f, 0f, 0f, new ScriptedRandom()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomEyesSchedule(0f, -1f, 0f, 0f, new ScriptedRandom()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomEyesSchedule(0f, 0f, -1f, 0f, new ScriptedRandom()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomEyesSchedule(0f, 0f, 0f, -1f, new ScriptedRandom()));
            Assert.Throws<ArgumentNullException>(() => new PhantomEyesSchedule(0f, 0f, 0f, 0f, null));
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
            }
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
            var schedule = new PhantomEyesSchedule(0.1f, 10f, 0f, Clearance, new SystemRandom(1));

            AllocationAssert.DoesNotAllocate(() =>
            {
                for (var i = 0; i < 64; i++)
                {
                    schedule.Tick(Frame, CritterMoment.Walking, NoEnemy, false);
                }
            });
        }
    }
}
