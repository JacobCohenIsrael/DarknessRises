using DarknessRises.Game.Runs;
using DarknessRises.Game.Saving;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Saving
{
    /// <summary>
    /// The best Score of any Run is kept in the save (version 4), survives an app restart, and a Run that beats it is
    /// told apart for the end screen's NEW BEST!.
    /// </summary>
    public sealed class BestScoreTests
    {
        private MemorySaveStorage storage;
        private RunFlow run;
        private PlayerSave save;
        private RunBanking banking;

        [SetUp]
        public void SetUp()
        {
            storage = new MemorySaveStorage();
            StartApp();
        }

        [TearDown]
        public void TearDown()
        {
            banking.Dispose();
        }

        private void StartApp()
        {
            banking?.Dispose();
            run = new RunFlow();
            save = PlayerSave.Load(storage);
            banking = new RunBanking(run, save);
        }

        private void PlayRunScoring(int score)
        {
            run.StartNewRun();
            run.AddScore(score);
            run.Exit();
            run.Leave();
        }

        [Test]
        public void A_version_3_save_keeps_everything_and_starts_with_a_best_Score_of_0()
        {
            // As version 3 wrote it, after the Adrenaline rename: Credits, Runs finished and what was bought.
            const string text = "{\"version\":3,\"credits\":420,\"runsFinished\":9,\"items\":["
                + "{\"id\":\"machine-gun\",\"level\":1},{\"id\":\"adrenaline.duration\",\"level\":2}]}";
            SaveFormat.TryParse(text, out var record);
            Assert.That(record.version, Is.EqualTo(3));

            SaveMigration.ToCurrent(record);

            Assert.That(record.version, Is.EqualTo(4));
            Assert.That(record.version, Is.EqualTo(SaveRecord.CurrentVersion));
            Assert.That(record.credits, Is.EqualTo(420));
            Assert.That(record.runsFinished, Is.EqualTo(9));
            Assert.That(record.items.Count, Is.EqualTo(2));
            Assert.That(record.items[0].id, Is.EqualTo("machine-gun"));
            Assert.That(record.items[1].id, Is.EqualTo("adrenaline.duration"));
            Assert.That(record.items[1].level, Is.EqualTo(2));
            Assert.That(record.bestScore, Is.Zero);
        }

        [Test]
        public void A_version_3_save_loads_with_its_bank_and_wares_and_no_best_Score()
        {
            storage.Text = "{\"version\":3,\"credits\":420,\"runsFinished\":9,\"items\":[{\"id\":\"machine-gun\",\"level\":1}]}";

            var loaded = PlayerSave.Load(storage);

            Assert.That(loaded.Credits, Is.EqualTo(420));
            Assert.That(loaded.RunsFinished, Is.EqualTo(9));
            Assert.That(loaded.LevelOf("machine-gun"), Is.EqualTo(1));
            Assert.That(loaded.BestScore, Is.Zero);
        }

        [Test]
        public void A_negative_best_Score_is_put_right()
        {
            var record = new SaveRecord { version = SaveRecord.CurrentVersion, bestScore = -5 };

            SaveMigration.ToCurrent(record);

            Assert.That(record.bestScore, Is.Zero);
        }

        [Test]
        public void The_best_Score_reads_back_as_it_was_written()
        {
            SaveFormat.TryParse(SaveFormat.ToText(new SaveRecord { bestScore = 12345 }), out var record);

            Assert.That(record.bestScore, Is.EqualTo(12345));
        }

        [Test]
        public void A_Run_that_ends_keeps_its_Score_as_the_best_across_an_app_restart()
        {
            PlayRunScoring(800);

            Assert.That(save.BestScore, Is.EqualTo(800));
            StartApp();
            Assert.That(save.BestScore, Is.EqualTo(800));
        }

        [Test]
        public void Only_a_better_Run_replaces_the_best()
        {
            PlayRunScoring(800);
            PlayRunScoring(500);

            Assert.That(save.BestScore, Is.EqualTo(800));
            Assert.That(run.Record.IsNewBest, Is.False);
            Assert.That(run.Record.BestScore, Is.EqualTo(800));

            PlayRunScoring(900);

            Assert.That(save.BestScore, Is.EqualTo(900));
            Assert.That(run.Record.IsNewBest, Is.True);
            Assert.That(run.Record.BestScoreBefore, Is.EqualTo(800));
        }

        [Test]
        public void The_first_Run_that_scores_is_a_new_best_but_one_that_scores_nothing_is_not()
        {
            PlayRunScoring(0);
            Assert.That(run.Record.IsNewBest, Is.False);

            PlayRunScoring(10);
            Assert.That(run.Record.IsNewBest, Is.True);
        }

        [Test]
        public void Equalling_the_best_is_not_a_new_best()
        {
            PlayRunScoring(800);
            PlayRunScoring(800);

            Assert.That(run.Record.IsNewBest, Is.False);
        }

        [Test]
        public void Restart_keeps_the_best_too()
        {
            run.StartNewRun();
            run.AddScore(300);
            var ended = run.Record;

            run.Restart();

            Assert.That(save.BestScore, Is.EqualTo(300));
            Assert.That(ended.IsNewBest, Is.True);
        }

        [Test]
        public void Going_to_the_background_keeps_the_Score_so_far_and_the_Run_is_still_a_new_best_when_it_ends()
        {
            PlayRunScoring(200);
            run.StartNewRun();
            run.AddScore(250);

            banking.BankNow();

            Assert.That(PlayerSave.Load(storage).BestScore, Is.EqualTo(250), "Kept, in case the app never comes back.");

            run.AddScore(50);
            run.Exit();

            Assert.That(save.BestScore, Is.EqualTo(300));
            Assert.That(run.Record.IsNewBest, Is.True, "Compared with the best before the Run, not with itself.");
            Assert.That(run.Record.BestScoreBefore, Is.EqualTo(200));
        }

        [Test]
        public void The_dev_tools_reset_clears_the_best()
        {
            PlayRunScoring(800);

            save.Reset();

            Assert.That(save.BestScore, Is.Zero);
        }

        [Test]
        public void A_Run_not_yet_ended_is_not_a_new_best()
        {
            run.StartNewRun();
            run.AddScore(99999);

            Assert.That(run.Record.IsNewBest, Is.False);
        }
    }
}
