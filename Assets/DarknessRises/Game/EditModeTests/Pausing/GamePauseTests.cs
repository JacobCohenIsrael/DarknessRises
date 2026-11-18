using System.Collections.Generic;
using DarknessRises.Game.Pausing;
using DarknessRises.Game.Runs;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Pausing
{
    public sealed class GamePauseTests
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
        }

        [Test]
        public void The_Level_plays_until_paused()
        {
            Assert.That(pause.State, Is.EqualTo(PauseState.Playing));
            Assert.That(pause.IsPaused, Is.False);
            Assert.That(pause.IsFrozen, Is.False);
        }

        [Test]
        public void Pausing_freezes_the_Level_and_brings_up_the_menu()
        {
            run.StartNewRun();

            Assert.That(pause.Pause(), Is.True);

            Assert.That(pause.IsPaused, Is.True);
            Assert.That(pause.IsFrozen, Is.True);
            Assert.That(states, Is.EqualTo(new[] { PauseState.Paused }));
        }

        [TestCase(RunPhase.LevelStart)]
        [TestCase(RunPhase.Walking)]
        [TestCase(RunPhase.StationFight)]
        [TestCase(RunPhase.BossFight)]
        [TestCase(RunPhase.LevelEnd)]
        public void The_game_pauses_in_every_part_of_a_Level(RunPhase phase)
        {
            run.StartNewRun();
            MoveTo(phase);

            Assert.That(run.Phase, Is.EqualTo(phase));
            Assert.That(pause.CanPause, Is.True);
            Assert.That(pause.Pause(), Is.True);
        }

        [Test]
        public void Nothing_pauses_before_a_Run()
        {
            Assert.That(pause.CanPause, Is.False);
            Assert.That(pause.Pause(), Is.False);
            Assert.That(states, Is.Empty);
        }

        [Test]
        public void Nothing_pauses_once_the_character_is_dead()
        {
            run.StartNewRun();
            run.Die();

            Assert.That(pause.Pause(), Is.False);
            Assert.That(pause.LoseFocus(), Is.False);
            Assert.That(pause.State, Is.EqualTo(PauseState.Playing));
        }

        [Test]
        public void Nothing_pauses_after_the_Run_ended()
        {
            run.StartNewRun();
            run.Die();
            run.Exit();

            Assert.That(pause.Pause(), Is.False);
        }

        [Test]
        public void Pausing_twice_pauses_once()
        {
            run.StartNewRun();
            pause.Pause();

            Assert.That(pause.Pause(), Is.False);
            Assert.That(states, Is.EqualTo(new[] { PauseState.Paused }));
        }

        [Test]
        public void Losing_focus_pauses()
        {
            run.StartNewRun();

            Assert.That(pause.LoseFocus(), Is.True);
            Assert.That(pause.IsPaused, Is.True);
        }

        [Test]
        public void Losing_focus_while_paused_keeps_the_menu_up()
        {
            run.StartNewRun();
            pause.Pause();

            Assert.That(pause.LoseFocus(), Is.False);
            Assert.That(pause.IsPaused, Is.True);
            Assert.That(states, Is.EqualTo(new[] { PauseState.Paused }));
        }

        [Test]
        public void Resume_carries_on_where_the_Level_froze()
        {
            run.StartNewRun();
            run.FinishStartBeat();
            run.BeginStationFight();
            pause.Pause();

            Assert.That(pause.Resume(), Is.True);

            Assert.That(pause.State, Is.EqualTo(PauseState.Playing));
            Assert.That(pause.IsFrozen, Is.False);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.StationFight));
            Assert.That(states, Is.EqualTo(new[] { PauseState.Paused, PauseState.Playing }));
        }

        [Test]
        public void Resume_does_nothing_unless_paused()
        {
            run.StartNewRun();

            Assert.That(pause.Resume(), Is.False);
            Assert.That(states, Is.Empty);
        }

        [Test]
        public void The_Pause_action_pauses_and_then_resumes()
        {
            run.StartNewRun();

            Assert.That(pause.Toggle(), Is.True);
            Assert.That(pause.IsPaused, Is.True);
            Assert.That(pause.Toggle(), Is.True);
            Assert.That(pause.IsPaused, Is.False);
        }

        [Test]
        public void The_game_pauses_again_after_resuming()
        {
            run.StartNewRun();
            pause.Pause();
            pause.Resume();

            Assert.That(pause.Pause(), Is.True);
        }

        [Test]
        public void Restart_from_the_pause_menu_starts_a_new_Run_from_Level_1()
        {
            run.StartNewRun();
            run.JumpToLevel(3);
            run.Record.CountKill();
            var levelsStarted = new List<int>();
            run.LevelStarted += levelsStarted.Add;
            pause.Pause();

            Assert.That(pause.Restart(), Is.True);

            Assert.That(levelsStarted, Is.EqualTo(new[] { 1 }));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.LevelStart));
            Assert.That(run.Record.EnemiesKilled, Is.Zero);
        }

        [Test]
        public void Exit_from_the_pause_menu_ends_the_Run_with_its_record()
        {
            run.StartNewRun();
            run.JumpToLevel(2);
            run.Record.CountKill();
            RunRecord ended = null;
            run.RunEnded += record => ended = record;
            pause.Pause();

            Assert.That(pause.Exit(), Is.True);

            Assert.That(run.Phase, Is.EqualTo(RunPhase.Ended));
            Assert.That(ended, Is.SameAs(run.Record));
            Assert.That(ended.LevelReached, Is.EqualTo(2));
            Assert.That(ended.EnemiesKilled, Is.EqualTo(1));
        }

        [Test]
        public void Restart_and_Exit_need_the_pause_menu()
        {
            run.StartNewRun();

            Assert.That(pause.Restart(), Is.False);
            Assert.That(pause.Exit(), Is.False);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.LevelStart));
        }

        [Test]
        public void A_Level_left_from_the_pause_menu_stays_frozen_until_it_goes()
        {
            run.StartNewRun();
            pause.Pause();

            pause.Restart();

            Assert.That(pause.State, Is.EqualTo(PauseState.Left));
            Assert.That(pause.IsFrozen, Is.True);
            Assert.That(pause.IsPaused, Is.False);
            Assert.That(states, Is.EqualTo(new[] { PauseState.Paused, PauseState.Left }));

            // The new Run is in play, but this Level is gone: nothing here pauses, resumes or leaves again.
            Assert.That(pause.CanPause, Is.False);
            Assert.That(pause.Pause(), Is.False);
            Assert.That(pause.Toggle(), Is.False);
            Assert.That(pause.Resume(), Is.False);
            Assert.That(pause.Exit(), Is.False);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.LevelStart));
        }

        [Test]
        public void After_Exit_the_end_screen_cannot_be_paused_or_resumed()
        {
            run.StartNewRun();
            pause.Pause();
            pause.Exit();

            Assert.That(pause.Toggle(), Is.False);
            Assert.That(pause.LoseFocus(), Is.False);
            Assert.That(pause.State, Is.EqualTo(PauseState.Left));
        }

        private void MoveTo(RunPhase phase)
        {
            switch (phase)
            {
                case RunPhase.Walking:
                    run.FinishStartBeat();
                    break;
                case RunPhase.StationFight:
                    run.FinishStartBeat();
                    run.BeginStationFight();
                    break;
                case RunPhase.BossFight:
                    run.FinishStartBeat();
                    run.BeginBossFight();
                    break;
                case RunPhase.LevelEnd:
                    run.FinishStartBeat();
                    run.BeginLevelEnd();
                    break;
            }
        }
    }
}
