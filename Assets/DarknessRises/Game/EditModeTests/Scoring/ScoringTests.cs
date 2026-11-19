using System;
using System.Collections.Generic;
using DarknessRises.Game.Scoring;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Scoring
{
    public sealed class ScoringTests
    {
        private const float Tolerance = 1e-4f;

        // Every 3 hits in a row the multiplier steps up, to 4 at most; 2 s without a hit ends the streak.
        private static Combo NewCombo() => new(3, 4, 2f);

        // S: 80 % accuracy, at most 5 s a kill, no hit taken. A: 60 %, 8 s, one hit. B: 40 %, any time, any hits.
        private static readonly GradeRequirement[] Grades =
        {
            new(Grade.S, 0.8f, 5f, 0),
            new(Grade.A, 0.6f, 8f, 1),
            new(Grade.B, 0.4f, 0f, -1),
        };

        private static ScoreKeeper NewKeeper() => new(NewCombo(), Grades, Grade.C);

        [Test]
        public void With_no_streak_the_multiplier_is_one()
        {
            var combo = NewCombo();

            Assert.That(combo.Multiplier, Is.EqualTo(1));
            Assert.That(combo.Streak, Is.Zero);
            Assert.That(combo.TimeLeftFraction, Is.Zero);
        }

        [Test]
        public void Consecutive_hits_step_the_multiplier_up()
        {
            var combo = NewCombo();
            var changes = new List<int>();
            combo.MultiplierChanged += changes.Add;

            for (var i = 0; i < 2; i++)
            {
                combo.Hit();
            }

            Assert.That(combo.Multiplier, Is.EqualTo(1));

            combo.Hit();
            Assert.That(combo.Multiplier, Is.EqualTo(2));

            for (var i = 0; i < 3; i++)
            {
                combo.Hit();
            }

            Assert.That(combo.Multiplier, Is.EqualTo(3));
            Assert.That(changes, Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void The_multiplier_stops_at_its_cap()
        {
            var combo = NewCombo();

            for (var i = 0; i < 100; i++)
            {
                combo.Hit();
            }

            Assert.That(combo.Multiplier, Is.EqualTo(4));
            Assert.That(combo.Streak, Is.EqualTo(100));
        }

        [Test]
        public void A_miss_ends_the_streak()
        {
            var combo = NewCombo();
            for (var i = 0; i < 7; i++)
            {
                combo.Hit();
            }

            var changes = new List<int>();
            combo.MultiplierChanged += changes.Add;
            combo.Miss();

            Assert.That(combo.Streak, Is.Zero);
            Assert.That(combo.Multiplier, Is.EqualTo(1));
            Assert.That(changes, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void A_miss_with_no_streak_changes_nothing()
        {
            var combo = NewCombo();
            var changed = false;
            combo.MultiplierChanged += _ => changed = true;

            combo.Miss();

            Assert.That(changed, Is.False);
        }

        [Test]
        public void Going_too_long_without_a_hit_ends_the_streak()
        {
            var combo = NewCombo();
            for (var i = 0; i < 3; i++)
            {
                combo.Hit();
            }

            combo.Tick(1f);
            Assert.That(combo.Multiplier, Is.EqualTo(2));
            Assert.That(combo.TimeLeftFraction, Is.EqualTo(0.5f).Within(Tolerance));

            combo.Tick(1f);
            Assert.That(combo.Streak, Is.Zero);
            Assert.That(combo.Multiplier, Is.EqualTo(1));
        }

        [Test]
        public void Each_hit_starts_the_timeout_over()
        {
            var combo = NewCombo();
            combo.Hit();
            combo.Tick(1.9f);

            combo.Hit();
            combo.Tick(1.9f);

            Assert.That(combo.Streak, Is.EqualTo(2));
        }

        [Test]
        public void A_Combo_with_no_timeout_only_ends_on_a_miss()
        {
            var combo = new Combo(3, 4, 0f);
            combo.Hit();

            combo.Tick(3600f);

            Assert.That(combo.Streak, Is.EqualTo(1));
            Assert.That(combo.TimeLeftFraction, Is.EqualTo(1f));
        }

        [Test]
        public void Ticking_the_Combo_does_not_allocate()
        {
            var combo = NewCombo();
            combo.Hit();

            AllocationAssert.DoesNotAllocate(() =>
            {
                combo.Hit();
                combo.Tick(0.001f);
            });
        }

        [Test]
        public void The_Combo_rejects_rules_that_make_no_sense()
        {
            Assert.That(() => new Combo(0, 4, 2f), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new Combo(3, 0, 2f), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new Combo(3, 4, -1f), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(0.9f, 4f, 0, Grade.S)]
        [TestCase(0.8f, 5f, 0, Grade.S)]
        [TestCase(0.9f, 4f, 1, Grade.A, Description = "A hit taken costs the S.")]
        [TestCase(0.9f, 6f, 0, Grade.A, Description = "Too slow for an S.")]
        [TestCase(0.7f, 4f, 0, Grade.A)]
        [TestCase(0.7f, 9f, 0, Grade.B, Description = "Too slow for an A; B has no time limit.")]
        [TestCase(0.5f, 60f, 5, Grade.B)]
        [TestCase(0.3f, 1f, 0, Grade.C)]
        public void A_fight_earns_the_best_Grade_it_meets(float accuracy, float secondsPerKill, int hitsTaken, Grade expected)
        {
            Assert.That(Grading.For(accuracy, secondsPerKill, hitsTaken, Grades, Grade.C), Is.EqualTo(expected));
        }

        [Test]
        public void The_order_the_Grades_are_authored_in_doesnt_matter()
        {
            var reversed = new[] { Grades[2], Grades[1], Grades[0] };

            Assert.That(Grading.For(0.9f, 4f, 0, reversed, Grade.C), Is.EqualTo(Grade.S));
        }

        [Test]
        public void With_no_requirements_every_fight_gets_the_lowest_Grade()
        {
            Assert.That(Grading.For(1f, 0f, 0, Array.Empty<GradeRequirement>(), Grade.C), Is.EqualTo(Grade.C));
        }

        [Test]
        public void Accuracy_is_the_share_of_Shots_that_hurt_an_Enemy()
        {
            Assert.That(StationScore.Accuracy(3, 1), Is.EqualTo(0.75f).Within(Tolerance));
            Assert.That(StationScore.Accuracy(0, 4), Is.Zero);
            Assert.That(StationScore.Accuracy(0, 0), Is.EqualTo(1f), "A fight won by Grenades missed nothing.");
        }

        [Test]
        public void A_kill_earns_its_points_times_the_Combo()
        {
            var keeper = NewKeeper();
            var scored = new List<int>();
            keeper.Scored += scored.Add;

            Assert.That(keeper.EnemyKilled(100), Is.EqualTo(100));

            for (var i = 0; i < 3; i++)
            {
                keeper.ShotHit();
            }

            Assert.That(keeper.EnemyKilled(100), Is.EqualTo(200));
            Assert.That(keeper.EnemyKilled(150), Is.EqualTo(300));
            Assert.That(scored, Is.EqualTo(new[] { 100, 200, 300 }));
        }

        [Test]
        public void A_miss_puts_the_next_kill_back_to_its_plain_points()
        {
            var keeper = NewKeeper();
            for (var i = 0; i < 6; i++)
            {
                keeper.ShotHit();
            }

            keeper.ShotMissed();

            Assert.That(keeper.EnemyKilled(100), Is.EqualTo(100));
        }

        [Test]
        public void A_Station_is_tallied_from_its_start_to_its_clear()
        {
            var keeper = NewKeeper();

            // Before the fight: these drive the Combo but aren't the Station's.
            keeper.ShotMissed();
            keeper.EnemyKilled(100);

            keeper.BeginStation();
            for (var i = 0; i < 4; i++)
            {
                keeper.ShotHit();
            }

            keeper.ShotMissed();
            keeper.EnemyKilled(100);
            keeper.EnemyKilled(100);
            keeper.CharacterHit();
            var result = keeper.EndStation(12f);

            Assert.That(result.Kills, Is.EqualTo(2));
            Assert.That(result.Hits, Is.EqualTo(4));
            Assert.That(result.Misses, Is.EqualTo(1));
            Assert.That(result.Accuracy, Is.EqualTo(0.8f).Within(Tolerance));
            Assert.That(result.HitsTaken, Is.EqualTo(1));
            Assert.That(result.Seconds, Is.EqualTo(12f));
            Assert.That(result.Points, Is.EqualTo(200), "Both kills came after the miss ended the streak.");
            Assert.That(result.Grade, Is.EqualTo(Grade.A), "80 % at 6 s a kill with one hit taken.");
            Assert.That(keeper.IsTallying, Is.False);
        }

        [Test]
        public void A_clean_fast_fight_earns_an_S()
        {
            var keeper = NewKeeper();
            keeper.BeginStation();
            for (var i = 0; i < 9; i++)
            {
                keeper.ShotHit();
            }

            keeper.ShotMissed();
            for (var i = 0; i < 3; i++)
            {
                keeper.EnemyKilled(100);
            }

            var result = keeper.EndStation(9f);

            Assert.That(result.Accuracy, Is.EqualTo(0.9f).Within(Tolerance));
            Assert.That(result.Grade, Is.EqualTo(Grade.S));
        }

        [Test]
        public void Each_Station_starts_its_tally_from_nothing()
        {
            var keeper = NewKeeper();
            keeper.BeginStation();
            keeper.ShotMissed();
            keeper.ShotMissed();
            keeper.EnemyKilled(100);
            keeper.EndStation(30f);

            keeper.BeginStation();
            keeper.ShotHit();
            keeper.EnemyKilled(100);
            var result = keeper.EndStation(2f);

            Assert.That(result.Kills, Is.EqualTo(1));
            Assert.That(result.Misses, Is.Zero);
            Assert.That(result.Accuracy, Is.EqualTo(1f));
        }

        [Test]
        public void Whats_shot_after_the_clear_isnt_the_Stations()
        {
            var keeper = NewKeeper();
            keeper.BeginStation();
            keeper.ShotHit();
            keeper.EnemyKilled(100);
            var result = keeper.EndStation(3f);

            keeper.ShotMissed();
            keeper.CharacterHit();
            keeper.EnemyKilled(100);

            keeper.BeginStation();
            var next = keeper.EndStation(0f);
            Assert.That(result.Misses, Is.Zero);
            Assert.That(next.Kills, Is.Zero);
            Assert.That(next.HitsTaken, Is.Zero);
        }

        [Test]
        public void The_keeper_runs_the_Combos_timeout()
        {
            var keeper = NewKeeper();
            for (var i = 0; i < 3; i++)
            {
                keeper.ShotHit();
            }

            keeper.Tick(2.1f);

            Assert.That(keeper.EnemyKilled(100), Is.EqualTo(100));
        }

        [Test]
        public void A_kill_worth_nothing_scores_nothing()
        {
            var keeper = NewKeeper();
            var scored = false;
            keeper.Scored += _ => scored = true;

            Assert.That(keeper.EnemyKilled(0), Is.Zero);
            Assert.That(scored, Is.False);
            Assert.That(() => keeper.EnemyKilled(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Keeping_Score_does_not_allocate()
        {
            var keeper = NewKeeper();
            keeper.BeginStation();

            AllocationAssert.DoesNotAllocate(() =>
            {
                keeper.ShotHit();
                keeper.ShotMissed();
                keeper.EnemyKilled(100);
                keeper.Tick(0.016f);
            });
        }
    }
}
