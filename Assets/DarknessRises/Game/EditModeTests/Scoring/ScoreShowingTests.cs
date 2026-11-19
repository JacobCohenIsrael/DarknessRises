using System;
using DarknessRises.Game.Hud;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Runs;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Scoring
{
    /// <summary>The Run's Score, the time a Station's fight took, and how the points and hit markers move on the HUD.</summary>
    public sealed class ScoreShowingTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void A_new_Run_has_no_Score()
        {
            Assert.That(new RunRecord().Score, Is.Zero);
        }

        [Test]
        public void The_Run_adds_up_what_its_kills_earn()
        {
            var run = new RunFlow();
            run.StartNewRun();

            Assert.That(run.AddScore(100), Is.True);
            Assert.That(run.AddScore(300), Is.True);

            Assert.That(run.Record.Score, Is.EqualTo(400));
        }

        [Test]
        public void Score_survives_into_the_next_Level_and_a_Continue()
        {
            var run = new RunFlow();
            run.StartNewRun();
            run.FinishStartBeat();
            run.AddScore(500);
            run.BeginLevelEnd();
            run.FinishLevel();
            run.FinishStartBeat();
            run.Die();
            run.Continue(1);

            Assert.That(run.Record.Score, Is.EqualTo(500));
        }

        [Test]
        public void A_new_Run_starts_its_Score_from_nothing()
        {
            var run = new RunFlow();
            run.StartNewRun();
            run.AddScore(500);

            run.Restart();

            Assert.That(run.Record.Score, Is.Zero);
        }

        [Test]
        public void Nothing_is_scored_outside_a_Run()
        {
            var run = new RunFlow();
            Assert.That(run.AddScore(100), Is.False, "before the first Run");

            run.StartNewRun();
            run.AddScore(100);
            run.Exit();

            Assert.That(run.AddScore(100), Is.False, "after Exit");
            Assert.That(run.Record.Score, Is.EqualTo(100), "The end screen still shows what the Run earned.");
        }

        [Test]
        public void Score_is_only_ever_earned()
        {
            Assert.That(() => new RunRecord().AddScore(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void The_rhythm_remembers_how_long_the_fight_took()
        {
            var rhythm = new StationRhythm(new StationRhythmTiming(2f, 1f, 0.5f, 1f));
            Assert.That(rhythm.FightSeconds, Is.Zero);

            rhythm.Arrive(new Station(0, 5f));
            rhythm.Tick(2f);
            rhythm.Tick(7.5f);
            rhythm.WinFight();
            rhythm.Tick(2.5f);

            Assert.That(rhythm.FightSeconds, Is.EqualTo(7.5f).Within(Tolerance), "The Ready beat and the beats after the fight don't count.");
        }

        [Test]
        public void Points_float_up_fast_at_first_and_stop_at_their_rise()
        {
            Assert.That(PopupMotion.Rise(0f, 90f), Is.Zero);
            Assert.That(PopupMotion.Rise(0.5f, 90f), Is.EqualTo(67.5f).Within(Tolerance));
            Assert.That(PopupMotion.Rise(1f, 90f), Is.EqualTo(90f).Within(Tolerance));
            Assert.That(PopupMotion.Rise(3f, 90f), Is.EqualTo(90f).Within(Tolerance));
        }

        [Test]
        public void Points_show_fully_and_fade_at_the_end_of_their_life()
        {
            Assert.That(PopupMotion.PointsAlpha(0f), Is.EqualTo(1f));
            Assert.That(PopupMotion.PointsAlpha(0.6f), Is.EqualTo(1f).Within(Tolerance));
            Assert.That(PopupMotion.PointsAlpha(0.8f), Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(PopupMotion.PointsAlpha(1f), Is.Zero);
        }

        [Test]
        public void A_hit_marker_shrinks_to_its_size_as_it_fades()
        {
            Assert.That(PopupMotion.MarkerScale(0f, 1.5f), Is.EqualTo(1.5f));
            Assert.That(PopupMotion.MarkerScale(1f, 1.5f), Is.EqualTo(1f));
            Assert.That(PopupMotion.MarkerAlpha(0f), Is.EqualTo(1f));
            Assert.That(PopupMotion.MarkerAlpha(0.25f), Is.EqualTo(0.75f).Within(Tolerance));
            Assert.That(PopupMotion.MarkerAlpha(1f), Is.Zero);
        }

        [Test]
        public void A_marks_life_runs_from_nothing_to_done()
        {
            Assert.That(PopupMotion.Progress(0f, 0.9f), Is.Zero);
            Assert.That(PopupMotion.Progress(0.45f, 0.9f), Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(PopupMotion.Progress(2f, 0.9f), Is.EqualTo(1f));
            Assert.That(PopupMotion.Progress(0.1f, 0f), Is.EqualTo(1f), "A mark with no life is over at once.");
        }
    }
}
