using System;
using System.Collections.Generic;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Saving;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Saving
{
    public sealed class PlayerSaveTests
    {
        private MemorySaveStorage storage;

        [SetUp]
        public void SetUp()
        {
            storage = new MemorySaveStorage();
        }

        private static RunCredits RunThatEarned(int credits)
        {
            var runCredits = new RunCredits();
            runCredits.Earn(credits);
            return runCredits;
        }

        [Test]
        public void With_nothing_stored_the_player_starts_from_nothing()
        {
            var save = PlayerSave.Load(storage);

            Assert.That(save.Credits, Is.Zero);
            Assert.That(save.RunsFinished, Is.Zero);
            Assert.That(storage.WriteCount, Is.Zero, "Loading writes nothing.");
        }

        [Test]
        public void Banking_moves_a_Runs_Credits_into_the_bank_and_saves()
        {
            var save = PlayerSave.Load(storage);
            var credits = RunThatEarned(120);

            Assert.That(save.Bank(credits), Is.EqualTo(120));

            Assert.That(save.Credits, Is.EqualTo(120));
            Assert.That(credits.Unbanked, Is.Zero);
            Assert.That(storage.WriteCount, Is.EqualTo(1));
        }

        [Test]
        public void Banking_again_banks_only_what_is_new()
        {
            var save = PlayerSave.Load(storage);
            var credits = RunThatEarned(120);
            save.Bank(credits);

            Assert.That(save.Bank(credits), Is.Zero);
            credits.Earn(30);
            credits.AddAdBonus(150);
            Assert.That(save.Bank(credits), Is.EqualTo(180));

            Assert.That(save.Credits, Is.EqualTo(300));
        }

        [Test]
        public void Banking_nothing_writes_nothing()
        {
            var save = PlayerSave.Load(storage);

            save.Bank(new RunCredits());

            Assert.That(storage.WriteCount, Is.Zero);
        }

        [Test]
        public void The_bank_survives_an_app_restart()
        {
            var save = PlayerSave.Load(storage);
            save.FinishRun(RunThatEarned(120));
            save.FinishRun(RunThatEarned(75));

            var afterRestart = PlayerSave.Load(storage);

            Assert.That(afterRestart.Credits, Is.EqualTo(195));
            Assert.That(afterRestart.RunsFinished, Is.EqualTo(2));
        }

        [Test]
        public void A_finished_Run_counts_even_when_it_earned_nothing()
        {
            var save = PlayerSave.Load(storage);

            save.FinishRun(new RunCredits());

            Assert.That(save.RunsFinished, Is.EqualTo(1));
            Assert.That(PlayerSave.Load(storage).RunsFinished, Is.EqualTo(1));
        }

        [Test]
        public void It_tells_when_the_Credits_in_the_bank_change()
        {
            var save = PlayerSave.Load(storage);
            var totals = new List<int>();
            save.CreditsChanged += totals.Add;

            save.Bank(RunThatEarned(40));
            save.Bank(new RunCredits());
            save.Bank(RunThatEarned(2));

            Assert.That(totals, Is.EqualTo(new[] { 40, 42 }));
        }

        [Test]
        public void A_failed_write_loses_nothing_and_is_made_up_at_the_next_save()
        {
            var save = PlayerSave.Load(storage);
            storage.FailsWrites = true;

            save.Bank(RunThatEarned(120));

            Assert.That(save.Credits, Is.EqualTo(120));
            Assert.That(save.Write(), Is.False);
            Assert.That(PlayerSave.Load(storage).Credits, Is.Zero, "Nothing reached the device yet.");

            storage.FailsWrites = false;
            Assert.That(save.Write(), Is.True);
            Assert.That(PlayerSave.Load(storage).Credits, Is.EqualTo(120));
        }

        [Test]
        public void Writing_an_unchanged_save_does_nothing()
        {
            var save = PlayerSave.Load(storage);
            save.Bank(RunThatEarned(5));

            Assert.That(save.Write(), Is.True);
            Assert.That(save.Write(), Is.True);

            Assert.That(storage.WriteCount, Is.EqualTo(1));
        }

        [Test]
        public void The_bank_stops_at_the_most_it_can_hold()
        {
            storage.Text = SaveFormat.ToText(new SaveRecord { credits = int.MaxValue - 10 });
            var save = PlayerSave.Load(storage);

            save.Bank(RunThatEarned(500));

            Assert.That(save.Credits, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void Text_that_isnt_a_save_is_read_as_no_save()
        {
            storage.Text = "{ not a save";

            var save = PlayerSave.Load(storage);

            Assert.That(save.Credits, Is.Zero);
            Assert.That(save.RunsFinished, Is.Zero);
        }

        [Test]
        public void It_needs_somewhere_to_keep_the_save_and_Credits_to_bank()
        {
            var save = PlayerSave.Load(storage);

            Assert.That(() => PlayerSave.Load(null), Throws.InstanceOf<ArgumentNullException>());
            Assert.That(() => save.Bank(null), Throws.InstanceOf<ArgumentNullException>());
            Assert.That(() => save.FinishRun(null), Throws.InstanceOf<ArgumentNullException>());
        }
    }
}
