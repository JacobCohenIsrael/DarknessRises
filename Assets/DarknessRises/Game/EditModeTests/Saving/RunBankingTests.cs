using DarknessRises.Game.Ads;
using DarknessRises.Game.Credits;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Saving;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Saving
{
    /// <summary>
    /// Credits are never lost: every way a Run can end banks them, and the bank survives an app restart. The save's
    /// file is swapped for one kept in memory; an app restart is a new <see cref="PlayerSave"/> read from the same storage.
    /// </summary>
    public sealed class RunBankingTests
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

        // What the boot does when the app starts: read the save, and bank every Run of a new Run flow in it.
        private void StartApp()
        {
            banking?.Dispose();
            run = new RunFlow();
            save = PlayerSave.Load(storage);
            banking = new RunBanking(run, save);
        }

        private int StoredCredits => PlayerSave.Load(storage).Credits;

        [Test]
        public void Exit_from_the_pause_menu_banks_the_Runs_Credits()
        {
            run.StartNewRun();
            run.EarnCredits(60);

            run.Exit();

            Assert.That(save.Credits, Is.EqualTo(60));
            Assert.That(save.RunsFinished, Is.EqualTo(1));
            Assert.That(StoredCredits, Is.EqualTo(60));
        }

        [Test]
        public void Game_over_banks_the_Runs_Credits()
        {
            run.StartNewRun();
            run.EarnCredits(45);
            run.Die();

            // The Continue countdown ran out, or the player took Exit on the death screen.
            run.Exit();

            Assert.That(save.Credits, Is.EqualTo(45));
            Assert.That(StoredCredits, Is.EqualTo(45));
        }

        [Test]
        public void Restart_banks_the_Runs_Credits_before_the_new_Run_starts()
        {
            run.StartNewRun();
            run.EarnCredits(30);

            run.Restart();

            Assert.That(save.Credits, Is.EqualTo(30));
            Assert.That(save.RunsFinished, Is.EqualTo(1));
            Assert.That(StoredCredits, Is.EqualTo(30));
            Assert.That(run.Record.Credits.Earned, Is.Zero, "The new Run starts with none.");
        }

        [Test]
        public void Restart_after_death_banks_too()
        {
            run.StartNewRun();
            run.EarnCredits(30);
            run.Die();

            run.Restart();

            Assert.That(StoredCredits, Is.EqualTo(30));
        }

        [Test]
        public void A_Continue_keeps_the_Run_and_its_Credits_going()
        {
            run.StartNewRun();
            run.FinishStartBeat();
            run.EarnCredits(30);
            run.Die();
            run.Continue(1);
            run.EarnCredits(20);
            run.Die();

            run.Exit();

            Assert.That(save.Credits, Is.EqualTo(50));
            Assert.That(save.RunsFinished, Is.EqualTo(1));
        }

        [Test]
        public void The_app_going_to_the_background_mid_Run_banks_what_it_earned_so_far()
        {
            run.StartNewRun();
            run.EarnCredits(25);

            banking.BankNow();

            Assert.That(StoredCredits, Is.EqualTo(25));
            Assert.That(save.RunsFinished, Is.Zero, "The Run isn't over.");
        }

        [Test]
        public void A_Run_that_carries_on_after_that_banks_only_the_rest()
        {
            run.StartNewRun();
            run.EarnCredits(25);
            banking.BankNow();
            run.EarnCredits(15);
            banking.BankNow();
            run.EarnCredits(10);

            run.Exit();

            Assert.That(save.Credits, Is.EqualTo(50));
            Assert.That(StoredCredits, Is.EqualTo(50));
        }

        [Test]
        public void An_app_killed_mid_Run_keeps_what_was_banked_on_the_way_out()
        {
            run.StartNewRun();
            run.EarnCredits(25);
            banking.BankNow();

            StartApp();

            Assert.That(save.Credits, Is.EqualTo(25));
        }

        [Test]
        public void Credits_add_up_across_two_Runs_and_an_app_restart()
        {
            run.StartNewRun();
            run.EarnCredits(120);
            run.Die();
            run.Exit();
            Assert.That(save.Credits, Is.EqualTo(120));

            run.StartNewRun();
            run.EarnCredits(80);
            run.Restart();
            Assert.That(save.Credits, Is.EqualTo(200));

            StartApp();

            Assert.That(save.Credits, Is.EqualTo(200));
            Assert.That(save.RunsFinished, Is.EqualTo(2));

            run.StartNewRun();
            run.EarnCredits(5);
            run.Exit();
            Assert.That(StoredCredits, Is.EqualTo(205));
        }

        [Test]
        public void The_end_of_Run_ad_banks_its_bonus_on_top()
        {
            run.StartNewRun();
            run.EarnCredits(150);
            run.Exit();
            var record = run.Record;
            var ad = new EndOfRunAd(record.Credits, factor: 2f, minimumCredits: 20);

            // What the end screen does once the ad was watched through.
            Assert.That(ad.Finish(AdOutcome.Watched), Is.EqualTo(150));
            save.Bank(record.Credits);

            Assert.That(save.Credits, Is.EqualTo(300));
            Assert.That(StoredCredits, Is.EqualTo(300));
            Assert.That(save.RunsFinished, Is.EqualTo(1), "The ad doesn't finish the Run a second time.");
        }

        [Test]
        public void An_ad_that_wasnt_watched_changes_nothing_in_the_bank()
        {
            run.StartNewRun();
            run.EarnCredits(150);
            run.Exit();
            var record = run.Record;
            var ad = new EndOfRunAd(record.Credits, factor: 2f, minimumCredits: 20);

            ad.Finish(AdOutcome.NotWatched);
            save.Bank(record.Credits);

            Assert.That(StoredCredits, Is.EqualTo(150));
        }

        [Test]
        public void Once_disposed_it_banks_no_more_Runs()
        {
            banking.Dispose();
            run.StartNewRun();
            run.EarnCredits(60);

            run.Exit();

            Assert.That(save.Credits, Is.Zero);
        }
    }
}
