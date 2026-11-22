using System.Collections.Generic;
using DarknessRises.Game.Credits;
using DarknessRises.Game.Runs;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Runs
{
    public sealed class RunFlowCreditsTests
    {
        private RunFlow run;

        [SetUp]
        public void SetUp()
        {
            run = new RunFlow();
        }

        [Test]
        public void The_clear_bonus_is_earned_only_while_a_Run_is_on()
        {
            Assert.That(run.EarnClearBonus(5), Is.False);

            run.StartNewRun();
            Assert.That(run.EarnClearBonus(5), Is.True);
            Assert.That(run.EarnClearBonus(8), Is.True);
            run.Die();
            Assert.That(run.EarnClearBonus(5), Is.True, "A Boss can still die to a Grenade in the air after the character did.");
            run.Exit();
            Assert.That(run.EarnClearBonus(5), Is.False);

            Assert.That(run.Record.Credits.Earned, Is.EqualTo(18));
        }

        [Test]
        public void The_Score_pays_Credits_at_the_rate_the_Level_hands_in()
        {
            run.StartNewRun();
            Assert.That(run.ConvertScoreAt(new ScoreToCredits(50)), Is.True);

            run.AddScore(520);
            run.EarnClearBonus(100);

            Assert.That(run.Record.Score, Is.EqualTo(520));
            Assert.That(run.Record.Credits.FromScore, Is.EqualTo(10));
            Assert.That(run.Record.Credits.Earned, Is.EqualTo(100));
            Assert.That(run.Record.Credits.Total, Is.EqualTo(110));
        }

        [Test]
        public void Every_new_Run_converts_at_the_rate_handed_in()
        {
            run.ConvertScoreAt(new ScoreToCredits(40));
            run.StartNewRun();
            run.AddScore(400);
            run.Exit();

            run.StartNewRun();
            run.AddScore(80);

            Assert.That(run.Record.Credits.ScorePerCredit, Is.EqualTo(40));
            Assert.That(run.Record.Credits.FromScore, Is.EqualTo(2));
        }

        [Test]
        public void A_Run_that_has_scored_keeps_its_rate()
        {
            run.StartNewRun();
            run.ConvertScoreAt(new ScoreToCredits(50));
            run.AddScore(100);

            Assert.That(run.ConvertScoreAt(new ScoreToCredits(50)), Is.True, "The next Level hands in the same rate.");
            Assert.That(run.ConvertScoreAt(new ScoreToCredits(10)), Is.False);
            Assert.That(run.Record.Credits.FromScore, Is.EqualTo(2));
        }

        [Test]
        public void Credits_carry_through_the_Levels_of_a_Run()
        {
            run.StartNewRun();
            run.EarnClearBonus(10);
            run.FinishStartBeat();
            run.BeginLevelEnd();
            run.FinishLevel();

            run.EarnClearBonus(7);

            Assert.That(run.LevelIndex, Is.EqualTo(2));
            Assert.That(run.Record.Credits.Earned, Is.EqualTo(17));
        }

        [Test]
        public void A_new_Run_starts_with_no_Credits()
        {
            run.StartNewRun();
            run.EarnClearBonus(10);
            run.Exit();

            run.StartNewRun();

            Assert.That(run.Record.Credits.Earned, Is.Zero);
        }

        [Test]
        public void Restart_ends_the_Run_with_its_record_before_the_new_one_starts()
        {
            var ended = new List<RunRecord>();
            run.RunEnded += ended.Add;
            run.StartNewRun();
            run.EarnClearBonus(25);
            var first = run.Record;

            run.Restart();

            Assert.That(ended, Is.EqualTo(new[] { first }));
            Assert.That(first.Credits.Earned, Is.EqualTo(25));
            Assert.That(run.Record, Is.Not.SameAs(first));
            Assert.That(run.Record.Credits.Earned, Is.Zero);
        }

        [Test]
        public void A_Run_ends_once_whichever_way_it_ends()
        {
            var ended = 0;
            run.RunEnded += _ => ended++;

            run.StartNewRun();
            run.Die();
            run.Exit();
            Assert.That(ended, Is.EqualTo(1));

            run.StartNewRun();
            run.Die();
            run.Restart();
            Assert.That(ended, Is.EqualTo(2));

            run.Exit();
            Assert.That(ended, Is.EqualTo(3));
            Assert.That(run.Exit(), Is.False);
            Assert.That(run.Restart(), Is.False);
            Assert.That(ended, Is.EqualTo(3));
        }
    }
}
