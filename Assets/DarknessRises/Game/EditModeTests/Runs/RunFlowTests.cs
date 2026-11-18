using System.Collections.Generic;
using DarknessRises.Game.Runs;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Runs
{
    public sealed class RunFlowTests
    {
        private RunFlow run;
        private List<int> levelsStarted;

        [SetUp]
        public void SetUp()
        {
            run = new RunFlow();
            levelsStarted = new List<int>();
            run.LevelStarted += levelsStarted.Add;
        }

        // Plays the current Level from its start beat to the far door, through one Station fight.
        private void PlayLevel()
        {
            run.FinishStartBeat();
            run.BeginStationFight();
            run.ClearStationFight();
            run.BeginLevelEnd();
            run.FinishLevel();
        }

        [Test]
        public void Nothing_is_in_play_before_the_first_Run()
        {
            Assert.That(run.Phase, Is.EqualTo(RunPhase.NotStarted));
            Assert.That(run.IsInRun, Is.False);
            Assert.That(run.FinishStartBeat(), Is.False);
        }

        [Test]
        public void A_new_Run_starts_Level_1_with_its_start_beat()
        {
            run.StartNewRun();

            Assert.That(run.Phase, Is.EqualTo(RunPhase.LevelStart));
            Assert.That(run.LevelIndex, Is.EqualTo(1));
            Assert.That(levelsStarted, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void A_Level_goes_from_its_start_beat_through_walks_and_Station_fights_to_its_end_beat()
        {
            var phases = new List<RunPhase>();
            run.StartNewRun();
            run.PhaseChanged += phases.Add;

            run.FinishStartBeat();
            run.BeginStationFight();
            run.ClearStationFight();
            run.BeginStationFight();
            run.ClearStationFight();
            run.BeginLevelEnd();

            Assert.That(phases, Is.EqualTo(new[]
            {
                RunPhase.Walking,
                RunPhase.StationFight,
                RunPhase.Walking,
                RunPhase.StationFight,
                RunPhase.Walking,
                RunPhase.LevelEnd,
            }));
        }

        [Test]
        public void A_Boss_fight_leads_to_the_end_beat()
        {
            run.StartNewRun();
            run.FinishStartBeat();

            Assert.That(run.BeginBossFight(), Is.True);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.BossFight));
            Assert.That(run.BeginLevelEnd(), Is.True);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.LevelEnd));
        }

        [Test]
        public void Finishing_a_Level_starts_the_next_one_straight_away()
        {
            run.StartNewRun();

            PlayLevel();
            PlayLevel();

            Assert.That(run.LevelIndex, Is.EqualTo(3));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.LevelStart));
            Assert.That(levelsStarted, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(run.Record.LevelReached, Is.EqualTo(3));
        }

        [Test]
        public void A_Level_only_finishes_from_its_end_beat()
        {
            run.StartNewRun();
            run.FinishStartBeat();
            run.BeginStationFight();

            Assert.That(run.FinishLevel(), Is.False);
            Assert.That(run.LevelIndex, Is.EqualTo(1));
            Assert.That(run.BeginLevelEnd(), Is.False, "The end beat waits for the fight to be cleared.");
        }

        [Test]
        public void Reports_that_dont_fit_the_phase_are_ignored()
        {
            run.StartNewRun();
            run.FinishStartBeat();
            run.Die();

            Assert.That(run.ClearStationFight(), Is.False);
            Assert.That(run.FinishStartBeat(), Is.False);
            Assert.That(run.FinishLevel(), Is.False);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Dead));
        }

        [TestCase(RunPhase.LevelStart)]
        [TestCase(RunPhase.Walking)]
        [TestCase(RunPhase.StationFight)]
        [TestCase(RunPhase.BossFight)]
        [TestCase(RunPhase.LevelEnd)]
        public void Continue_goes_back_to_the_phase_the_character_died_in(RunPhase phase)
        {
            run.StartNewRun();
            if (phase != RunPhase.LevelStart)
            {
                run.FinishStartBeat();
            }

            if (phase == RunPhase.StationFight)
            {
                run.BeginStationFight();
            }
            else if (phase == RunPhase.BossFight)
            {
                run.BeginBossFight();
            }
            else if (phase == RunPhase.LevelEnd)
            {
                run.BeginLevelEnd();
            }

            run.Die();

            Assert.That(run.CanContinue(1), Is.True);
            Assert.That(run.Continue(1), Is.True);
            Assert.That(run.Phase, Is.EqualTo(phase));
        }

        [Test]
        public void Continue_stays_in_the_same_Level_with_its_record()
        {
            run.StartNewRun();
            PlayLevel();
            run.FinishStartBeat();
            run.CountKill();
            run.Die();
            levelsStarted.Clear();

            run.Continue(1);

            Assert.That(run.LevelIndex, Is.EqualTo(2));
            Assert.That(run.Record.LevelReached, Is.EqualTo(2));
            Assert.That(run.Record.EnemiesKilled, Is.EqualTo(1));
            Assert.That(levelsStarted, Is.Empty, "No Level is loaded again.");
        }

        [Test]
        public void Continue_is_allowed_once_per_Run()
        {
            run.StartNewRun();
            run.FinishStartBeat();
            run.Die();
            run.Continue(1);
            run.BeginStationFight();
            run.Die();

            Assert.That(run.CanContinue(1), Is.False);
            Assert.That(run.Continue(1), Is.False);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Dead));
            Assert.That(run.ContinuesUsed, Is.EqualTo(1));
        }

        [Test]
        public void The_authored_allowance_sets_how_many_Continues_a_Run_has()
        {
            run.StartNewRun();
            run.Die();
            Assert.That(run.CanContinue(0), Is.False, "No Continues at all.");

            Assert.That(run.Continue(2), Is.True);
            run.Die();
            Assert.That(run.Continue(2), Is.True);
            run.Die();
            Assert.That(run.Continue(2), Is.False);
        }

        [Test]
        public void Continue_only_brings_back_a_dead_character()
        {
            Assert.That(run.Continue(1), Is.False, "No Run yet.");

            run.StartNewRun();
            run.FinishStartBeat();

            Assert.That(run.CanContinue(1), Is.False);
            Assert.That(run.Continue(1), Is.False);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Walking));
            Assert.That(run.ContinuesUsed, Is.Zero);
        }

        [Test]
        public void Restart_gives_the_new_Run_its_Continue_back()
        {
            run.StartNewRun();
            run.Die();
            run.Continue(1);
            run.Die();

            run.Restart();
            run.Die();

            Assert.That(run.ContinuesUsed, Is.Zero);
            Assert.That(run.CanContinue(1), Is.True);
        }

        [Test]
        public void A_new_Run_after_Exit_has_its_Continue_back()
        {
            run.StartNewRun();
            run.Die();
            run.Continue(1);
            run.Die();
            run.Exit();

            run.StartNewRun();
            run.Die();

            Assert.That(run.CanContinue(1), Is.True);
        }

        [Test]
        public void The_next_Level_of_the_same_Run_doesnt_give_the_Continue_back()
        {
            run.StartNewRun();
            run.FinishStartBeat();
            run.Die();
            run.Continue(1);
            run.BeginLevelEnd();
            run.FinishLevel();
            run.Die();

            Assert.That(run.CanContinue(1), Is.False);
        }

        [Test]
        public void While_dead_the_Run_follows_the_Level_so_Continue_goes_back_to_where_it_is_now()
        {
            run.StartNewRun();
            run.FinishStartBeat();
            run.BeginStationFight();
            run.Die();

            // A Grenade still in the air kills the Station's last Enemy.
            Assert.That(run.ClearStationFight(), Is.True);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Dead));

            run.Continue(1);

            Assert.That(run.Phase, Is.EqualTo(RunPhase.Walking));
            Assert.That(run.BeginStationFight(), Is.True, "The next Station fights as usual.");
        }

        [Test]
        public void A_Continue_announces_the_phase_it_goes_back_to()
        {
            var phases = new List<RunPhase>();
            run.StartNewRun();
            run.FinishStartBeat();
            run.Die();
            run.PhaseChanged += phases.Add;

            run.Continue(1);

            Assert.That(phases, Is.EqualTo(new[] { RunPhase.Walking }));
        }

        [TestCase(RunPhase.LevelStart)]
        [TestCase(RunPhase.Walking)]
        [TestCase(RunPhase.StationFight)]
        [TestCase(RunPhase.LevelEnd)]
        public void The_character_can_die_in_any_part_of_a_Level(RunPhase phase)
        {
            run.StartNewRun();
            if (phase != RunPhase.LevelStart)
            {
                run.FinishStartBeat();
            }

            if (phase == RunPhase.StationFight)
            {
                run.BeginStationFight();
            }
            else if (phase == RunPhase.LevelEnd)
            {
                run.BeginLevelEnd();
            }

            Assert.That(run.Phase, Is.EqualTo(phase));
            Assert.That(run.Die(), Is.True);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Dead));
        }

        [Test]
        public void Counts_Enemies_killed_through_the_whole_Run()
        {
            run.StartNewRun();
            run.CountKill();
            PlayLevel();
            run.CountKill();
            run.Die();
            run.CountKill();

            Assert.That(run.Record.EnemiesKilled, Is.EqualTo(3), "Kills after death (a Grenade still in the air) count too.");
        }

        [Test]
        public void Restart_starts_a_new_Run_from_Level_1()
        {
            run.StartNewRun();
            run.CountKill();
            PlayLevel();
            PlayLevel();
            run.FinishStartBeat();
            run.Die();
            levelsStarted.Clear();

            Assert.That(run.Restart(), Is.True);

            Assert.That(run.LevelIndex, Is.EqualTo(1));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.LevelStart));
            Assert.That(run.Record.LevelReached, Is.EqualTo(1));
            Assert.That(run.Record.EnemiesKilled, Is.Zero);
            Assert.That(levelsStarted, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void Restart_works_mid_Level_for_the_pause_menu()
        {
            run.StartNewRun();
            PlayLevel();
            run.FinishStartBeat();

            Assert.That(run.Restart(), Is.True);
            Assert.That(run.LevelIndex, Is.EqualTo(1));
        }

        [Test]
        public void Exit_ends_the_Run_with_the_real_Level_reached_and_Enemies_killed()
        {
            RunRecord ended = null;
            run.RunEnded += record => ended = record;
            run.StartNewRun();
            run.CountKill();
            PlayLevel();
            run.CountKill();
            run.CountKill();
            run.FinishStartBeat();
            run.Die();

            Assert.That(run.Exit(), Is.True);

            Assert.That(run.Phase, Is.EqualTo(RunPhase.Ended));
            Assert.That(run.IsInRun, Is.False);
            Assert.That(ended, Is.SameAs(run.Record));
            Assert.That(ended.LevelReached, Is.EqualTo(2));
            Assert.That(ended.EnemiesKilled, Is.EqualTo(3));
        }

        [Test]
        public void After_Exit_nothing_moves_the_Run_until_a_new_one_starts()
        {
            run.StartNewRun();
            run.Die();
            run.Exit();

            Assert.That(run.Restart(), Is.False);
            Assert.That(run.CountKill(), Is.False);
            Assert.That(run.JumpToLevel(4), Is.False);
            Assert.That(run.Record.EnemiesKilled, Is.Zero);

            Assert.That(run.StartNewRun(), Is.True);
            Assert.That(run.LevelIndex, Is.EqualTo(1));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.LevelStart));
        }

        [Test]
        public void A_new_Run_cant_start_over_a_Run_in_play()
        {
            run.StartNewRun();
            PlayLevel();

            Assert.That(run.StartNewRun(), Is.False);
            Assert.That(run.LevelIndex, Is.EqualTo(2));
        }

        [Test]
        public void Jumping_to_a_Level_starts_it_with_the_same_Run()
        {
            run.StartNewRun();
            run.CountKill();
            run.FinishStartBeat();

            Assert.That(run.JumpToLevel(6), Is.True);

            Assert.That(run.LevelIndex, Is.EqualTo(6));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.LevelStart));
            Assert.That(levelsStarted, Is.EqualTo(new[] { 1, 6 }));
            Assert.That(run.Record.LevelReached, Is.EqualTo(6));
            Assert.That(run.Record.EnemiesKilled, Is.EqualTo(1));

            PlayLevel();
            Assert.That(run.LevelIndex, Is.EqualTo(7));
        }

        [Test]
        public void Levels_count_from_1()
        {
            run.StartNewRun();

            Assert.That(() => run.JumpToLevel(0), Throws.InstanceOf<System.ArgumentOutOfRangeException>());
        }
    }
}
