using System;
using DarknessRises.Game.Critters;
using DarknessRises.Game.EditModeTests.Levels;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Runs;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Critters
{
    public sealed class CritterScheduleTests
    {
        private const float Frame = 1f / 64f;

        // Walking 0.1/s, after a clear 1/s, during a Wave fight 0.01/s.
        private static readonly CritterRates Rates = new(0.1f, 1f, 0.01f);

        // A random source that always says "now": the lowest roll there is.
        private static ScriptedRandom AlwaysNow()
        {
            var random = new ScriptedRandom();
            for (var i = 0; i < 100000; i++)
            {
                random.Next(0f);
            }

            return random;
        }

        private static ScriptedRandom Never() => new();

        [TestCase(RunPhase.Walking, StationBeat.Walking, CritterMoment.Walking)]
        [TestCase(RunPhase.StationFight, StationBeat.Ready, CritterMoment.WaveFight)]
        [TestCase(RunPhase.StationFight, StationBeat.Fight, CritterMoment.WaveFight)]
        [TestCase(RunPhase.StationFight, StationBeat.Clear, CritterMoment.AfterClear)]
        [TestCase(RunPhase.StationFight, StationBeat.Quiet, CritterMoment.AfterClear)]
        [TestCase(RunPhase.StationFight, StationBeat.Go, CritterMoment.AfterClear)]
        [TestCase(RunPhase.BossFight, StationBeat.Walking, CritterMoment.BossFight)]
        [TestCase(RunPhase.BossFight, StationBeat.Fight, CritterMoment.BossFight)]
        [TestCase(RunPhase.LevelStart, StationBeat.Walking, CritterMoment.None)]
        [TestCase(RunPhase.LevelEnd, StationBeat.Walking, CritterMoment.None)]
        [TestCase(RunPhase.Dead, StationBeat.Fight, CritterMoment.None)]
        [TestCase(RunPhase.Ended, StationBeat.Walking, CritterMoment.None)]
        [TestCase(RunPhase.NotStarted, StationBeat.Walking, CritterMoment.None)]
        public void The_moment_comes_from_the_Run_and_the_Station_rhythm(RunPhase phase, StationBeat beat, CritterMoment expected)
        {
            Assert.That(CritterMoments.Of(phase, beat), Is.EqualTo(expected));
        }

        [Test]
        public void A_Boss_fight_never_has_a_Critter_however_lucky()
        {
            var schedule = new CritterSchedule(new CritterRates(100f, 100f, 100f), 0f, 0f, 5, AlwaysNow());

            for (var t = 0f; t < 60f; t += Frame)
            {
                Assert.That(schedule.Tick(Frame, CritterMoment.BossFight, 0), Is.False);
            }
        }

        [Test]
        public void No_moment_has_no_Critter()
        {
            var schedule = new CritterSchedule(Rates, 0f, 0f, 1, AlwaysNow());

            Assert.That(schedule.Tick(Frame, CritterMoment.None, 0), Is.False);
        }

        [TestCase(CritterMoment.Walking)]
        [TestCase(CritterMoment.AfterClear)]
        [TestCase(CritterMoment.WaveFight)]
        public void A_Critter_can_come_in_the_moments_that_allow_one(CritterMoment moment)
        {
            var schedule = new CritterSchedule(Rates, 0f, 0f, 1, AlwaysNow());

            Assert.That(schedule.Tick(Frame, moment, 0), Is.True);
        }

        [Test]
        public void A_roll_above_the_chance_brings_none()
        {
            var schedule = new CritterSchedule(Rates, 0f, 0f, 1, Never());

            for (var t = 0f; t < 10f; t += Frame)
            {
                Assert.That(schedule.Tick(Frame, CritterMoment.AfterClear, 0), Is.False);
            }
        }

        [Test]
        public void The_chance_follows_the_moment_s_rate_and_the_frame()
        {
            // At 1 per second, the chance in one 1/64 s frame is 1 - e^(-1/64), about 0.0155: a roll just under comes.
            var under = new ScriptedRandom();
            under.Next(0.015f);
            var over = new ScriptedRandom();
            over.Next(0.016f);

            Assert.That(new CritterSchedule(Rates, 0f, 0f, 1, under).Tick(Frame, CritterMoment.AfterClear, 0), Is.True);
            Assert.That(new CritterSchedule(Rates, 0f, 0f, 1, over).Tick(Frame, CritterMoment.AfterClear, 0), Is.False);

            // Walking is ten times rarer: the same roll that comes after a clear doesn't while walking.
            var walking = new ScriptedRandom();
            walking.Next(0.015f);
            Assert.That(new CritterSchedule(Rates, 0f, 0f, 1, walking).Tick(Frame, CritterMoment.Walking, 0), Is.False);
        }

        [Test]
        public void Over_time_Critters_come_most_after_a_clear_and_rarely_in_a_fight()
        {
            int Count(CritterMoment moment)
            {
                var schedule = new CritterSchedule(Rates, 0f, 0f, 1, new Randomness.SystemRandom(7));
                var count = 0;
                for (var t = 0f; t < 600f; t += Frame)
                {
                    count += schedule.Tick(Frame, moment, 0) ? 1 : 0;
                }

                return count;
            }

            var afterClear = Count(CritterMoment.AfterClear);
            var walking = Count(CritterMoment.Walking);
            var fight = Count(CritterMoment.WaveFight);

            Assert.That(afterClear, Is.InRange(480, 720));
            Assert.That(walking, Is.InRange(40, 90));
            Assert.That(fight, Is.LessThan(walking));
        }

        [Test]
        public void After_a_Critter_the_cooldown_must_pass()
        {
            var schedule = new CritterSchedule(Rates, 2f, 0f, 5, AlwaysNow());
            Assert.That(schedule.Tick(Frame, CritterMoment.AfterClear, 0), Is.True);

            for (var t = 0f; t < 2f - Frame; t += Frame)
            {
                Assert.That(schedule.Tick(Frame, CritterMoment.AfterClear, 0), Is.False);
            }

            Assert.That(schedule.Tick(Frame, CritterMoment.AfterClear, 0), Is.False);
            Assert.That(schedule.Tick(Frame, CritterMoment.AfterClear, 0), Is.True);
        }

        [Test]
        public void The_cooldown_runs_whatever_the_moment()
        {
            var schedule = new CritterSchedule(Rates, 1f, 0f, 5, AlwaysNow());
            schedule.Tick(Frame, CritterMoment.Walking, 0);

            schedule.Tick(1f, CritterMoment.BossFight, 0);

            Assert.That(schedule.CooldownLeft, Is.Zero);
            Assert.That(schedule.Tick(Frame, CritterMoment.Walking, 0), Is.True);
        }

        [Test]
        public void None_come_before_the_first_delay()
        {
            var schedule = new CritterSchedule(Rates, 0f, 3f, 1, AlwaysNow());

            for (var t = 0f; t < 3f; t += Frame)
            {
                Assert.That(schedule.Tick(Frame, CritterMoment.AfterClear, 0), Is.False);
            }

            Assert.That(schedule.Tick(Frame, CritterMoment.AfterClear, 0), Is.True);
        }

        [Test]
        public void None_come_while_the_cap_is_out()
        {
            var schedule = new CritterSchedule(Rates, 0f, 0f, 2, AlwaysNow());

            Assert.That(schedule.Tick(Frame, CritterMoment.AfterClear, 2), Is.False);
            Assert.That(schedule.Tick(Frame, CritterMoment.AfterClear, 1), Is.True);
        }

        [Test]
        public void A_paused_game_brings_none()
        {
            var schedule = new CritterSchedule(Rates, 0f, 0f, 1, AlwaysNow());

            Assert.That(schedule.Tick(0f, CritterMoment.AfterClear, 0), Is.False);
        }

        [Test]
        public void Bad_settings_are_refused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CritterRates(-1f, 0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CritterSchedule(Rates, -1f, 0f, 1, Never()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CritterSchedule(Rates, 0f, -1f, 1, Never()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CritterSchedule(Rates, 0f, 0f, -1, Never()));
            Assert.Throws<ArgumentNullException>(() => new CritterSchedule(Rates, 0f, 0f, 1, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CritterSchedule(Rates, 0f, 0f, 1, Never()).Tick(-1f, CritterMoment.Walking, 0));
        }

        [Test]
        public void Ticking_does_not_allocate()
        {
            var schedule = new CritterSchedule(Rates, 1f, 0f, 1, new Randomness.SystemRandom(3));

            AllocationAssert.DoesNotAllocate(() =>
            {
                for (var i = 0; i < 64; i++)
                {
                    schedule.Tick(Frame, CritterMoment.Walking, 0);
                }
            });
        }
    }
}
