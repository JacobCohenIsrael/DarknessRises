using System;
using DarknessRises.Game.Hud;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Scoring;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Scoring
{
    /// <summary>
    /// The Score boost: offered while none runs, started by an ad watched to the end, doubling every Score earned for
    /// its time of play, carried by the Run from Level to Level and ended with it.
    /// </summary>
    public sealed class ScoreBoostTests
    {
        private static readonly GradeRequirement[] Grades = { new(Grade.S, 0.8f, 0f, -1) };

        private static ScoreBoost NewBoost(int maxPerRun = 0) => new(2, 60f, maxPerRun);

        [Test]
        public void A_new_boost_is_offered_and_not_running()
        {
            var boost = NewBoost();

            Assert.That(boost.IsOffered, Is.True);
            Assert.That(boost.IsRunning, Is.False);
            Assert.That(boost.SecondsLeft, Is.Zero);
            Assert.That(boost.Apply(30), Is.EqualTo(30));
        }

        [Test]
        public void Watching_the_ad_starts_it_and_no_card_offers_another_while_it_runs()
        {
            var boost = NewBoost();
            var started = 0;
            boost.Started += () => started++;

            Assert.That(boost.Start(), Is.True);

            Assert.That(boost.IsRunning, Is.True);
            Assert.That(boost.SecondsLeft, Is.EqualTo(60f));
            Assert.That(boost.IsOffered, Is.False, "The player's rule: no button while the timer is up.");
            Assert.That(boost.Start(), Is.False);
            Assert.That(started, Is.EqualTo(1));
        }

        [Test]
        public void While_it_runs_every_Score_is_doubled()
        {
            var boost = NewBoost();
            boost.Start();

            Assert.That(boost.Apply(30), Is.EqualTo(60));
            Assert.That(boost.Apply(0), Is.Zero);
        }

        [Test]
        public void Its_time_runs_down_with_play_and_then_it_ends_and_is_offered_again()
        {
            var boost = NewBoost();
            var ended = 0;
            boost.Ended += () => ended++;
            boost.Start();

            boost.Tick(59f);
            Assert.That(boost.IsRunning, Is.True);
            Assert.That(boost.SecondsLeft, Is.EqualTo(1f).Within(1e-4f));

            boost.Tick(0f);
            Assert.That(boost.SecondsLeft, Is.EqualTo(1f).Within(1e-4f), "No play time, no change: paused, an ad, a load.");

            boost.Tick(1.5f);
            Assert.That(boost.IsRunning, Is.False);
            Assert.That(boost.SecondsLeft, Is.Zero);
            Assert.That(ended, Is.EqualTo(1));
            Assert.That(boost.TimeRanOut, Is.True);
            Assert.That(boost.IsOffered, Is.True);
            Assert.That(boost.Apply(30), Is.EqualTo(30));
        }

        [Test]
        public void It_pulses_as_it_is_about_to_end()
        {
            var boost = NewBoost();
            Assert.That(boost.IsEnding(10f), Is.False, "Not running.");

            boost.Start();
            boost.Tick(49f);
            Assert.That(boost.IsEnding(10f), Is.False);

            boost.Tick(1.5f);
            Assert.That(boost.IsEnding(10f), Is.True);
        }

        [Test]
        public void An_authored_limit_per_Run_stops_the_offer()
        {
            var boost = NewBoost(maxPerRun: 1);
            boost.Start();
            boost.Tick(61f);

            Assert.That(boost.IsOffered, Is.False);
            Assert.That(boost.Start(), Is.False);
            Assert.That(boost.StartedCount, Is.EqualTo(1));
        }

        [Test]
        public void Ending_a_boost_that_isnt_running_reports_nothing()
        {
            var boost = NewBoost();
            var ended = 0;
            boost.Ended += () => ended++;

            boost.End();

            Assert.That(ended, Is.Zero);
        }

        [Test]
        public void Nonsense_numbers_are_refused()
        {
            Assert.That(() => new ScoreBoost(0, 60f, 0), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new ScoreBoost(2, 0f, 0), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new ScoreBoost(2, 60f, -1), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => NewBoost().Tick(-1f), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void A_boosted_kill_is_doubled_after_the_Combo()
        {
            var boost = NewBoost();
            var keeper = new ScoreKeeper(new Combo(2, 3, 0f), Grades, Grade.C, AccuracyBonus.None, boost);
            keeper.ShotHit();
            keeper.ShotHit();

            Assert.That(keeper.EnemyKilled(10), Is.EqualTo(20), "Combo x2.");

            boost.Start();

            Assert.That(keeper.IsBoosted, Is.True);
            Assert.That(keeper.EnemyKilled(10), Is.EqualTo(40), "Combo x2, then the boost x2.");
        }

        [Test]
        public void A_boosted_kills_share_of_the_accuracy_bonus_is_boosted_too()
        {
            var boost = NewBoost();
            var keeper = new ScoreKeeper(
                new Combo(100, 1, 0f), Grades, Grade.C, new AccuracyBonus(new[] { new AccuracyTier(1f, 1.5f) }, 1), boost);
            keeper.BeginStation();
            keeper.ShotHit();
            keeper.EnemyKilled(20);
            boost.Start();
            keeper.ShotHit();
            keeper.EnemyKilled(20);

            var result = keeper.EndStation(4f);

            Assert.That(result.Points, Is.EqualTo(60), "20, then 20 doubled.");
            Assert.That(result.Bonus, Is.EqualTo(30), "Half of 60: 10 from the plain kill, 20 from the boosted one.");
        }

        [Test]
        public void The_Run_carries_its_boost_into_the_next_Level()
        {
            var run = new RunFlow();
            run.StartNewRun();
            var boost = NewBoost();

            Assert.That(run.CarryBoost(boost), Is.True);
            boost.Start();
            run.FinishStartBeat();
            run.BeginLevelEnd();
            run.FinishLevel();

            Assert.That(run.ScoreBoost, Is.SameAs(boost));
            Assert.That(boost.IsRunning, Is.True);
        }

        [Test]
        public void The_boost_ends_with_the_Run()
        {
            var run = new RunFlow();
            run.StartNewRun();
            var boost = NewBoost();
            run.CarryBoost(boost);
            boost.Start();

            run.Exit();

            Assert.That(boost.IsRunning, Is.False);
            Assert.That(boost.TimeRanOut, Is.False);
        }

        [Test]
        public void A_new_Run_starts_without_a_boost()
        {
            var run = new RunFlow();
            run.StartNewRun();
            var boost = NewBoost();
            run.CarryBoost(boost);
            boost.Start();

            run.Restart();

            Assert.That(boost.IsRunning, Is.False);
            Assert.That(run.ScoreBoost, Is.Null);
        }

        [Test]
        public void Outside_a_Run_nothing_carries_a_boost()
        {
            Assert.That(new RunFlow().CarryBoost(NewBoost()), Is.False);
        }

        [Test]
        public void The_authored_boost_doubles_for_a_minute_with_no_limit()
        {
            var boost = new ScoreSettings().ToBoost();

            Assert.That(boost.Factor, Is.EqualTo(2));
            Assert.That(boost.DurationSeconds, Is.EqualTo(60f));
            for (var i = 0; i < 5; i++)
            {
                Assert.That(boost.Start(), Is.True);
                boost.Tick(61f);
            }
        }

        [Test]
        public void Running_the_timer_down_allocates_nothing()
        {
            var boost = NewBoost();
            boost.Start();

            AllocationAssert.DoesNotAllocate(() =>
            {
                for (var i = 0; i < 100; i++)
                {
                    boost.Tick(0.01f);
                    boost.Apply(10);
                }
            });
        }

        [Test]
        public void The_card_shows_the_Score_then_stamps_the_bonus_then_counts_to_the_total()
        {
            var reveal = new BonusReveal(0.5f, 0.25f, 2f, 0.125f, 0.5f);
            var stamped = 0;
            var counted = 0;
            reveal.Stamped += () => stamped++;
            reveal.CountStarted += () => counted++;

            reveal.Begin();
            reveal.Tick(0.25f);
            Assert.That(reveal.IsStamped, Is.False, "The Station's own Score reads first.");

            reveal.Tick(0.25f);
            Assert.That(reveal.IsStamped, Is.True);
            Assert.That(reveal.StampSize, Is.EqualTo(2f).Within(1e-4f), "It punches in from large.");

            reveal.Tick(0.25f);
            Assert.That(reveal.StampSize, Is.EqualTo(1f).Within(1e-4f), "Landed.");
            Assert.That(reveal.IsCounting, Is.False);

            reveal.Tick(0.125f);
            Assert.That(reveal.IsCounting, Is.True);
            Assert.That(stamped, Is.EqualTo(1));
            Assert.That(counted, Is.EqualTo(1));
            Assert.That(reveal.TotalSeconds, Is.EqualTo(1.375f).Within(1e-4f));

            reveal.Tick(1f);
            Assert.That(counted, Is.EqualTo(1), "Once per reveal.");
        }

        [Test]
        public void A_card_without_a_bonus_never_stamps()
        {
            var reveal = new BonusReveal(0.4f, 0.2f, 2f, 0.1f, 0.5f);
            reveal.Begin();
            reveal.Stop();

            reveal.Tick(5f);

            Assert.That(reveal.IsStamped, Is.False);
            Assert.That(reveal.IsCounting, Is.False);
        }

        [Test]
        public void The_reveal_allocates_nothing_as_it_plays()
        {
            var reveal = new BonusReveal(0.4f, 0.2f, 2f, 0.1f, 0.5f);

            AllocationAssert.DoesNotAllocate(() =>
            {
                reveal.Begin();
                for (var i = 0; i < 100; i++)
                {
                    reveal.Tick(0.02f);
                }
            });
        }
    }
}
