using DarknessRises.Game.Runs;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Runs
{
    /// <summary>Leaving the end screen: what hands an ended Run over to the Shop.</summary>
    public sealed class RunFlowLeaveTests
    {
        private RunFlow run;
        private int left;

        [SetUp]
        public void SetUp()
        {
            run = new RunFlow();
            left = 0;
            run.Left += () => left++;
        }

        [Test]
        public void An_ended_Run_is_left_once()
        {
            run.StartNewRun();
            run.Die();
            run.Exit();

            Assert.That(run.Leave(), Is.True);
            Assert.That(run.Leave(), Is.False);

            Assert.That(left, Is.EqualTo(1));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Ended), "Leaving doesn't start anything: the Shop comes first.");
        }

        [Test]
        public void A_Run_that_is_still_on_cant_be_left()
        {
            Assert.That(run.Leave(), Is.False, "Before any Run.");

            run.StartNewRun();
            Assert.That(run.Leave(), Is.False);
            run.Die();
            Assert.That(run.Leave(), Is.False);

            Assert.That(left, Is.Zero);
        }

        [Test]
        public void Restart_never_leaves_it_starts_the_new_Run_at_once()
        {
            run.StartNewRun();

            run.Restart();

            Assert.That(left, Is.Zero);
            Assert.That(run.IsInRun, Is.True);
            Assert.That(run.Leave(), Is.False);
        }

        [Test]
        public void Each_ended_Run_can_be_left()
        {
            run.StartNewRun();
            run.Exit();
            run.Leave();

            run.StartNewRun();
            run.Exit();

            Assert.That(run.Leave(), Is.True);
            Assert.That(left, Is.EqualTo(2));
        }

        [Test]
        public void A_new_Run_starts_after_the_last_one_was_left()
        {
            run.StartNewRun();
            run.Exit();
            run.Leave();

            Assert.That(run.StartNewRun(), Is.True);
            Assert.That(run.LevelIndex, Is.EqualTo(1));
            Assert.That(run.Record.Credits.Earned, Is.Zero);
        }
    }
}
