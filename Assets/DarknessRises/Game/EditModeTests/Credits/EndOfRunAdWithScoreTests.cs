using DarknessRises.Game.Ads;
using DarknessRises.Game.Credits;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Saving;
using DarknessRises.Game.Scoring;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Credits
{
    /// <summary>
    /// The end-of-Run ad came back (#133) on top of Credits from the Score (#127): it doubles everything the Run's play
    /// earned, its Score's Credits and its Level clear bonus, once per Run, and banking around it never counts twice.
    /// </summary>
    public sealed class EndOfRunAdWithScoreTests
    {
        private const int Rate = 35;
        private const int Minimum = 20;

        private MemorySaveStorage storage;
        private PlayerSave save;
        private RunFlow run;
        private RunBanking banking;

        [SetUp]
        public void SetUp()
        {
            storage = new MemorySaveStorage();
            save = PlayerSave.Load(storage);
            run = new RunFlow();
            run.ConvertScoreAt(new ScoreToCredits(Rate));
            banking = new RunBanking(run, save);
        }

        [TearDown]
        public void TearDown() => banking.Dispose();

        [Test]
        public void It_doubles_the_Score_Credits_and_the_clear_bonus()
        {
            run.StartNewRun();
            run.AddScore(42300);
            run.EarnClearBonus(250);
            run.Exit();
            var credits = run.Record.Credits;
            var ad = new EndOfRunAd(credits, 2f, Minimum);

            Assert.That(credits.Earnings, Is.EqualTo(1208 + 250));
            Assert.That(ad.Bonus, Is.EqualTo(1458));
            Assert.That(ad.Finish(AdOutcome.Watched), Is.EqualTo(1458));
            Assert.That(credits.Total, Is.EqualTo(2916));
        }

        [Test]
        public void It_pays_once_per_Run()
        {
            run.StartNewRun();
            run.AddScore(3500);
            run.Exit();
            var credits = run.Record.Credits;

            Assert.That(new EndOfRunAd(credits, 2f, Minimum).Finish(AdOutcome.Watched), Is.EqualTo(100));
            Assert.That(new EndOfRunAd(credits, 2f, Minimum).IsOffered, Is.False);
            Assert.That(new EndOfRunAd(credits, 2f, Minimum).Finish(AdOutcome.Watched), Is.Zero);
            Assert.That(credits.Total, Is.EqualTo(200));
        }

        [Test]
        public void A_Run_whose_Score_paid_under_the_minimum_isnt_offered_it()
        {
            run.StartNewRun();
            run.AddScore(19 * Rate + Rate - 1);
            run.Exit();

            Assert.That(run.Record.Credits.Earnings, Is.EqualTo(19));
            Assert.That(new EndOfRunAd(run.Record.Credits, 2f, Minimum).IsOffered, Is.False);

            run.StartNewRun();
            run.AddScore(20 * Rate);
            run.Exit();

            Assert.That(new EndOfRunAd(run.Record.Credits, 2f, Minimum).IsOffered, Is.True);
        }

        [Test]
        public void Banking_mid_Run_at_the_end_and_after_the_ad_never_counts_twice()
        {
            run.StartNewRun();
            run.AddScore(1240);
            banking.BankNow();
            Assert.That(save.Credits, Is.EqualTo(35));

            run.AddScore(2000);
            run.EarnClearBonus(100);
            run.Exit();
            Assert.That(save.Credits, Is.EqualTo(92 + 100));

            var ad = new EndOfRunAd(run.Record.Credits, 2f, Minimum);
            ad.Finish(AdOutcome.Watched);
            save.Bank(run.Record.Credits);
            save.Bank(run.Record.Credits);

            Assert.That(save.Credits, Is.EqualTo(2 * 192));
            Assert.That(PlayerSave.Load(storage).Credits, Is.EqualTo(2 * 192));
            Assert.That(save.RunsFinished, Is.EqualTo(1), "The ad doesn't finish the Run a second time.");
        }

        [Test]
        public void An_ad_that_wasnt_watched_banks_nothing_more()
        {
            run.StartNewRun();
            run.AddScore(3500);
            run.Exit();

            new EndOfRunAd(run.Record.Credits, 2f, Minimum).Finish(AdOutcome.NotWatched);
            save.Bank(run.Record.Credits);

            Assert.That(PlayerSave.Load(storage).Credits, Is.EqualTo(100));
        }

        [Test]
        public void The_Score_boost_and_the_end_ad_stack()
        {
            run.StartNewRun();
            var boost = new ScoreBoost(2, 120f, 0);
            boost.Start();
            run.AddScore(boost.Apply(1750));
            run.Exit();
            var credits = run.Record.Credits;

            new EndOfRunAd(credits, 2f, Minimum).Finish(AdOutcome.Watched);

            Assert.That(credits.Total, Is.EqualTo(4 * (1750 / Rate)));
        }

        [Test]
        public void The_Score_boost_lasts_120_seconds_of_play()
        {
            var boost = new ScoreSettings().ToBoost();
            boost.Start();

            Assert.That(boost.DurationSeconds, Is.EqualTo(120f));
            boost.Tick(119.9f);
            Assert.That(boost.IsRunning, Is.True);
            boost.Tick(0.2f);
            Assert.That(boost.IsRunning, Is.False);
            Assert.That(boost.TimeRanOut, Is.True);
        }
    }
}
