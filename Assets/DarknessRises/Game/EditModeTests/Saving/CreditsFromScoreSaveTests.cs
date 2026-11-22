using DarknessRises.Game.Credits;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Saving;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Saving
{
    /// <summary>
    /// Credits come from the Score now (#127) and the end-of-Run x2 Credits ad is gone. The save never recorded anything
    /// of that ad: its bonus was banked as plain Credits. So a save from before loads as it was, with no new version.
    /// </summary>
    public sealed class CreditsFromScoreSaveTests
    {
        // As build 0.1.18 wrote it, after a Run whose end-of-Run ad doubled its Credits.
        private const string SaveFromTheAdDays =
            "{\"version\":4,\"credits\":1460,\"runsFinished\":6,\"items\":[{\"id\":\"machine-gun\",\"level\":1},"
            + "{\"id\":\"pistol.magazine\",\"level\":2}],\"bestScore\":18250}";

        [Test]
        public void A_save_from_the_days_of_the_end_of_Run_ad_loads_with_everything()
        {
            var save = PlayerSave.Load(new MemorySaveStorage { Text = SaveFromTheAdDays });

            Assert.That(save.Credits, Is.EqualTo(1460));
            Assert.That(save.RunsFinished, Is.EqualTo(6));
            Assert.That(save.BestScore, Is.EqualTo(18250));
            Assert.That(save.LevelOf("machine-gun"), Is.EqualTo(1));
            Assert.That(save.LevelOf("pistol.magazine"), Is.EqualTo(2));
        }

        [Test]
        public void A_save_that_still_names_an_ad_bonus_loads_and_drops_it()
        {
            // Should any build have written what the ad paid, the field is read past and not written again.
            var storage = new MemorySaveStorage
            {
                Text = "{\"version\":4,\"credits\":300,\"runsFinished\":1,\"items\":[],\"bestScore\":900,\"adBonus\":150,\"hasAdBonus\":true}",
            };

            var save = PlayerSave.Load(storage);
            Assert.That(save.Credits, Is.EqualTo(300), "What the ad paid is in the bank already; nothing is added or taken.");

            var run = new RunCredits(new ScoreToCredits(50));
            run.AddScore(500);
            save.Bank(run);

            Assert.That(PlayerSave.Load(storage).Credits, Is.EqualTo(310));
            Assert.That(storage.Text, Does.Not.Contain("adBonus"));
        }

        [Test]
        public void A_loaded_old_save_banks_Score_Credits_on_top()
        {
            var storage = new MemorySaveStorage { Text = SaveFromTheAdDays };
            var save = PlayerSave.Load(storage);
            var run = new RunFlow();
            using var banking = new RunBanking(run, save);
            run.ConvertScoreAt(new ScoreToCredits(50));

            run.StartNewRun();
            run.AddScore(2549);
            run.EarnClearBonus(100);
            run.Exit();

            Assert.That(PlayerSave.Load(storage).Credits, Is.EqualTo(1460 + 50 + 100));
        }
    }
}
