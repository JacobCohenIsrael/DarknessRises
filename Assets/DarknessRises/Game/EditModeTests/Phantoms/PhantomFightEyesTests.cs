using System;
using DarknessRises.Game.Critters;
using DarknessRises.Game.EditModeTests.Levels;
using DarknessRises.Game.Phantoms;
using DarknessRises.Game.Randomness;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Phantoms
{
    // #106: a smaller Enemy clearance measured at the eyes' own spot too, and rare far-edge eyes in Wave fights.
    public sealed class PhantomFightEyesTests
    {
        private const float Frame = 1f / 64f;
        private const float Clearance = 9f;
        private const float SpotClearance = 6f;
        private const float KeepAway = 7.5f;
        private const float NoEnemy = float.PositiveInfinity;

        // Gaps of exactly gapSeconds (an empty ScriptedRandom gives the most of each range).
        private static PhantomFightSchedule Schedule(float gapSeconds = 1f, int maxPairs = 1, float retry = 0.25f) =>
            new(gapSeconds, gapSeconds, retry, maxPairs, new ScriptedRandom());

        private static int Count(PhantomFightSchedule schedule, float seconds, CritterMoment moment, int pairsOut = 0)
        {
            var count = 0;
            for (var t = 0f; t < seconds; t += Frame)
            {
                count += schedule.Tick(Frame, moment, pairsOut) ? 1 : 0;
            }

            return count;
        }

        // Opens at once, blinks out over 0.125 s; 9 m from the character, 6 m from its spot, 7.5 m keep-away.
        private static PhantomEyesSighting Shown(bool inFight, float holdSeconds = 4f)
        {
            var sighting = new PhantomEyesSighting(0f, 0.125f, Clearance, KeepAway, default, null, SpotClearance);
            sighting.Show(holdSeconds, inFight);
            return sighting;
        }

        private static PhantomConditions In(
            CritterMoment moment,
            float nearestToCharacter = NoEnemy,
            float nearestToSpot = NoEnemy,
            bool lit = false,
            bool aimed = false,
            float metresAway = 11f) =>
            new(moment, nearestToCharacter, lit, aimed, metresAway, nearestToSpot);

        private static PhantomVanish Run(PhantomEyesSighting sighting, float seconds, in PhantomConditions conditions)
        {
            for (var t = 0f; t < seconds; t += Frame)
            {
                var vanish = sighting.Tick(Frame, conditions);
                if (vanish != PhantomVanish.None)
                {
                    return vanish;
                }
            }

            return PhantomVanish.None;
        }

        [TestCase(CritterMoment.WaveFight, true)]
        [TestCase(CritterMoment.BossFight, false)]
        [TestCase(CritterMoment.Walking, false)]
        [TestCase(CritterMoment.AfterClear, false)]
        [TestCase(CritterMoment.None, false)]
        public void Fight_eyes_come_only_in_a_Wave_fight_never_the_Boss_fight(CritterMoment moment, bool mayShow)
        {
            Assert.That(PhantomFightSchedule.MayShow(moment), Is.EqualTo(mayShow));
            Assert.That(Count(Schedule(), 10f, moment), mayShow ? Is.GreaterThan(0) : Is.Zero);
        }

        [Test]
        public void Only_Wave_fight_time_counts_toward_the_gap()
        {
            var schedule = Schedule(gapSeconds: 2f);
            Assert.That(Count(schedule, 1.5f, CritterMoment.WaveFight), Is.Zero);
            Assert.That(Count(schedule, 30f, CritterMoment.Walking), Is.Zero);
            Assert.That(Count(schedule, 30f, CritterMoment.BossFight), Is.Zero);
            Assert.That(Count(schedule, 0.6f, CritterMoment.WaveFight), Is.EqualTo(1));
        }

        [Test]
        public void None_is_due_while_the_most_fight_pairs_are_out()
        {
            var schedule = Schedule(gapSeconds: 1f, maxPairs: 1);
            Assert.That(Count(schedule, 10f, CritterMoment.WaveFight, pairsOut: 1), Is.Zero);
            Assert.That(schedule.Tick(Frame, CritterMoment.WaveFight, 0), Is.True);
        }

        [Test]
        public void No_fight_pairs_when_the_most_is_zero()
        {
            Assert.That(Count(Schedule(gapSeconds: 0.5f, maxPairs: 0), 20f, CritterMoment.WaveFight), Is.Zero);
        }

        [Test]
        public void A_missed_pair_tries_again_soon_not_a_whole_gap_later()
        {
            var schedule = Schedule(gapSeconds: 10f, retry: 0.5f);
            Assert.That(Count(schedule, 10.1f, CritterMoment.WaveFight), Is.EqualTo(1));
            schedule.Missed();
            Assert.That(schedule.GapLeft, Is.EqualTo(0.5f));
            Assert.That(Count(schedule, 0.6f, CritterMoment.WaveFight), Is.EqualTo(1));
        }

        [Test]
        public void Over_ten_minutes_of_Wave_fight_a_pair_comes_every_eight_to_fifteen_seconds()
        {
            // The authored tuning: 8-15 s of fight between pairs.
            var schedule = new PhantomFightSchedule(8f, 15f, 1f, 1, new SystemRandom(7));
            var pairs = Count(schedule, 600f, CritterMoment.WaveFight);
            Assert.That(pairs, Is.InRange(40, 75));
        }

        [Test]
        public void Fight_eyes_stay_with_Enemies_about_while_their_own_spot_is_clear()
        {
            var sighting = Shown(inFight: true);
            Assert.That(Run(sighting, 2f, In(CritterMoment.WaveFight, nearestToCharacter: 3f, nearestToSpot: 8f)), Is.EqualTo(PhantomVanish.None));
            Assert.That(sighting.IsShowing, Is.True);
        }

        [Test]
        public void Eyes_never_sit_next_to_a_real_Enemy_in_a_fight_or_not()
        {
            var fight = Shown(inFight: true);
            Assert.That(fight.Tick(Frame, In(CritterMoment.WaveFight, nearestToCharacter: 3f, nearestToSpot: SpotClearance)), Is.EqualTo(PhantomVanish.EnemyNear));

            var safe = Shown(inFight: false);
            Assert.That(safe.Tick(Frame, In(CritterMoment.Walking, nearestToCharacter: 12f, nearestToSpot: 5f)), Is.EqualTo(PhantomVanish.EnemyNear));
        }

        [Test]
        public void Safe_time_eyes_stay_with_an_Enemy_beyond_the_smaller_clearance_and_go_within_it()
        {
            var sighting = Shown(inFight: false);
            Assert.That(sighting.Tick(Frame, In(CritterMoment.Walking, nearestToCharacter: 10f, nearestToSpot: 8f)), Is.EqualTo(PhantomVanish.None));
            Assert.That(sighting.Tick(Frame, In(CritterMoment.Walking, nearestToCharacter: Clearance, nearestToSpot: 8f)), Is.EqualTo(PhantomVanish.EnemyNear));
        }

        [TestCase(CritterMoment.BossFight)]
        [TestCase(CritterMoment.AfterClear)]
        [TestCase(CritterMoment.None)]
        public void Fight_eyes_go_at_once_when_the_Wave_fight_ends_or_the_Boss_fight_starts(CritterMoment next)
        {
            var sighting = Shown(inFight: true);
            Assert.That(sighting.Tick(Frame, In(next)), Is.EqualTo(PhantomVanish.MomentEnded));
            Assert.That(sighting.Strength, Is.Zero);
        }

        [Test]
        public void Safe_time_eyes_still_go_when_a_fight_starts()
        {
            var sighting = Shown(inFight: false);
            Assert.That(sighting.Tick(Frame, In(CritterMoment.WaveFight)), Is.EqualTo(PhantomVanish.MomentEnded));
        }

        [Test]
        public void Fight_eyes_are_gone_in_the_very_frame_light_reaches_them()
        {
            var sighting = Shown(inFight: true);
            Assert.That(sighting.Tick(Frame, In(CritterMoment.WaveFight, nearestToCharacter: 4f, lit: true)), Is.EqualTo(PhantomVanish.Lit));
            Assert.That(sighting.IsShowing, Is.False);
        }

        [Test]
        public void Fight_eyes_blink_out_when_aimed_at_or_when_the_character_comes_near()
        {
            var aimed = Shown(inFight: true);
            Assert.That(Run(aimed, 0.5f, In(CritterMoment.WaveFight, aimed: true)), Is.EqualTo(PhantomVanish.AimedAt));

            var near = Shown(inFight: true);
            Assert.That(Run(near, 0.5f, In(CritterMoment.WaveFight, metresAway: KeepAway - 0.5f)), Is.EqualTo(PhantomVanish.TooNear));
        }

        [Test]
        public void The_authored_tuning_keeps_every_phantom_out_of_the_near_range()
        {
            var definition = ScriptableObject.CreateInstance<PhantomEyesDefinition>();
            try
            {
                var character = Vector3.zero;
                var safe = definition.ToSpot();
                var fight = definition.ToFightSpot();

                // Beyond the keep-away distance, which is beyond where real eyes (7 m) and Critters (7.5 m) glint.
                Assert.That(definition.KeepAwayMetres, Is.GreaterThanOrEqualTo(7.5f));
                Assert.That(safe.MinMetres, Is.GreaterThan(definition.KeepAwayMetres));
                Assert.That(fight.MinMetres, Is.GreaterThanOrEqualTo(safe.MinMetres));
                Assert.That(fight.IsFarOut(new Vector3(0f, 0f, definition.KeepAwayMetres), character), Is.False);
                Assert.That(fight.IsFarOut(new Vector3(0f, 0f, fight.MinMetres - 0.5f), character), Is.False);
                Assert.That(fight.IsFarOut(new Vector3(0f, 0f, (fight.MinMetres + fight.MaxMetres) / 2f), character), Is.True);

                // Rare and capped in fights, and a phantom keeps its distance from real Enemies.
                Assert.That(definition.FightMaxPairs, Is.InRange(1, 1));
                Assert.That(definition.SpotClearanceMetres, Is.GreaterThan(0f));
                Assert.That(definition.EnemyClearanceMetres, Is.GreaterThan(definition.KeepAwayMetres));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void Negative_gaps_or_pairs_are_refused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomFightSchedule(-1f, 2f, 1f, 1, new ScriptedRandom()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomFightSchedule(3f, 2f, 1f, 1, new ScriptedRandom()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomFightSchedule(1f, 2f, 1f, -1, new ScriptedRandom()));
            Assert.Throws<ArgumentOutOfRangeException>(() => Schedule().Tick(-Frame, CritterMoment.WaveFight, 0));
        }

        [Test]
        public void Ticking_the_fight_schedule_and_fight_eyes_does_not_allocate()
        {
            var schedule = new PhantomFightSchedule(1f, 2f, 0.5f, 1, new SystemRandom(1));
            var sighting = Shown(inFight: true, holdSeconds: 1f);
            var conditions = In(CritterMoment.WaveFight, nearestToCharacter: 4f, nearestToSpot: 9f);
            AllocationAssert.DoesNotAllocate(() =>
            {
                for (var i = 0; i < 640; i++)
                {
                    if (schedule.Tick(Frame, CritterMoment.WaveFight, sighting.IsShowing ? 1 : 0))
                    {
                        sighting.Show(1f, true);
                    }

                    sighting.Tick(Frame, conditions);
                }
            });
        }
    }
}
