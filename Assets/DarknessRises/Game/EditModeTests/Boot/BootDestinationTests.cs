using System;
using DarknessRises.Game.Boot;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Saving;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Boot
{
    public sealed class BootDestinationTests
    {
        [Test]
        public void A_first_install_boots_straight_into_Level_1()
        {
            var save = PlayerSave.Load(new MemorySaveStorage());

            Assert.That(BootDestinations.For(save), Is.EqualTo(BootDestination.FirstRun));
        }

        [Test]
        public void Once_a_Run_has_been_finished_the_app_opens_in_the_Shop()
        {
            var storage = new MemorySaveStorage();
            var save = PlayerSave.Load(storage);
            save.FinishRun(new RunCredits());

            Assert.That(BootDestinations.For(save), Is.EqualTo(BootDestination.Shop));
            Assert.That(BootDestinations.For(PlayerSave.Load(storage)), Is.EqualTo(BootDestination.Shop), "After an app restart too.");
        }

        [Test]
        public void A_Run_that_earned_nothing_counts_as_finished()
        {
            var run = new RunFlow();
            var save = PlayerSave.Load(new MemorySaveStorage());
            using var banking = new RunBanking(run, save);
            run.StartNewRun();

            run.Exit();

            Assert.That(BootDestinations.For(save), Is.EqualTo(BootDestination.Shop));
        }

        [Test]
        public void Restart_finishes_a_Run_too()
        {
            var run = new RunFlow();
            var save = PlayerSave.Load(new MemorySaveStorage());
            using var banking = new RunBanking(run, save);
            run.StartNewRun();

            run.Restart();

            Assert.That(BootDestinations.For(save), Is.EqualTo(BootDestination.Shop));
        }

        [Test]
        public void An_app_closed_in_the_middle_of_the_first_Run_still_boots_into_Level_1()
        {
            var storage = new MemorySaveStorage();
            var run = new RunFlow();
            var save = PlayerSave.Load(storage);
            using var banking = new RunBanking(run, save);
            run.StartNewRun();
            run.EarnClearBonus(40);

            banking.BankNow();

            var afterRestart = PlayerSave.Load(storage);
            Assert.That(afterRestart.Credits, Is.EqualTo(40), "What it earned is kept.");
            Assert.That(BootDestinations.For(afterRestart), Is.EqualTo(BootDestination.FirstRun));
        }

        [Test]
        public void It_needs_a_save()
        {
            Assert.That(() => BootDestinations.For(null), Throws.InstanceOf<ArgumentNullException>());
        }
    }
}
