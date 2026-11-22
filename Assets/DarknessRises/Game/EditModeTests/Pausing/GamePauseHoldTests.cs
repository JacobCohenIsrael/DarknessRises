using System.Collections.Generic;
using DarknessRises.Game.Pausing;
using DarknessRises.Game.Runs;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Pausing
{
    /// <summary>The Level held still, without the pause menu, while the Score boost's ad plays over it.</summary>
    public sealed class GamePauseHoldTests
    {
        private RunFlow run;
        private GamePause pause;
        private List<PauseState> states;

        [SetUp]
        public void SetUp()
        {
            run = new RunFlow();
            pause = new GamePause(run);
            states = new List<PauseState>();
            pause.StateChanged += states.Add;
            run.StartNewRun();
        }

        [Test]
        public void Holding_freezes_the_Level_without_the_pause_menu()
        {
            Assert.That(pause.Hold(), Is.True);

            Assert.That(pause.IsFrozen, Is.True);
            Assert.That(pause.IsPaused, Is.False, "No pause menu over the ad.");
            Assert.That(states, Is.EqualTo(new[] { PauseState.Held }));
        }

        [Test]
        public void Releasing_lets_the_Level_carry_on()
        {
            pause.Hold();

            Assert.That(pause.Release(), Is.True);

            Assert.That(pause.IsFrozen, Is.False);
            Assert.That(states, Is.EqualTo(new[] { PauseState.Held, PauseState.Playing }));
        }

        [Test]
        public void While_held_the_player_cant_pause_and_losing_focus_changes_nothing()
        {
            pause.Hold();

            Assert.That(pause.CanPause, Is.False);
            Assert.That(pause.Pause(), Is.False);
            Assert.That(pause.Toggle(), Is.False);
            Assert.That(pause.LoseFocus(), Is.False);
            Assert.That(pause.State, Is.EqualTo(PauseState.Held));
        }

        [Test]
        public void Nothing_holds_a_paused_or_dead_Level_and_only_a_held_one_is_released()
        {
            Assert.That(pause.Release(), Is.False);

            pause.Pause();
            Assert.That(pause.Hold(), Is.False);
            Assert.That(pause.Release(), Is.False);
            pause.Resume();

            run.Die();
            Assert.That(pause.Hold(), Is.False);
        }
    }
}
