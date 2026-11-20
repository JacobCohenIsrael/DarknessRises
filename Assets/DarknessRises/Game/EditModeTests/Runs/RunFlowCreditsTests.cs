using System.Collections.Generic;
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
        public void Credits_are_earned_only_while_a_Run_is_on()
        {
            Assert.That(run.EarnCredits(5), Is.False);

            run.StartNewRun();
            Assert.That(run.EarnCredits(5), Is.True);
            Assert.That(run.EarnCredits(8), Is.True);
            run.Die();
            Assert.That(run.EarnCredits(5), Is.True, "A Grenade still in the air can kill after the character died.");
            run.Exit();
            Assert.That(run.EarnCredits(5), Is.False);

            Assert.That(run.Record.Credits.Earned, Is.EqualTo(18));
        }

        [Test]
        public void Credits_are_not_the_Score()
        {
            run.StartNewRun();

            run.AddScore(500);
            run.EarnCredits(5);

            Assert.That(run.Record.Score, Is.EqualTo(500));
            Assert.That(run.Record.Credits.Earned, Is.EqualTo(5));
        }

        [Test]
        public void Credits_carry_through_the_Levels_of_a_Run()
        {
            run.StartNewRun();
            run.EarnCredits(10);
            run.FinishStartBeat();
            run.BeginLevelEnd();
            run.FinishLevel();

            run.EarnCredits(7);

            Assert.That(run.LevelIndex, Is.EqualTo(2));
            Assert.That(run.Record.Credits.Earned, Is.EqualTo(17));
        }

        [Test]
        public void A_new_Run_starts_with_no_Credits()
        {
            run.StartNewRun();
            run.EarnCredits(10);
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
            run.EarnCredits(25);
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
