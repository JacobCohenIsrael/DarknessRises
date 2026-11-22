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
            credits.AddScore(150);
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
        public void A_purchase_pays_from_the_bank_and_raises_the_item()
        {
            var save = PlayerSave.Load(storage);
            save.Bank(RunThatEarned(300));

            Assert.That(save.Purchase("pistol.magazine", 1, 250), Is.True);

            Assert.That(save.Credits, Is.EqualTo(50));
            Assert.That(save.LevelOf("pistol.magazine"), Is.EqualTo(1));
            Assert.That(save.LevelOf("something-else"), Is.Zero);
        }

        [Test]
        public void A_purchase_the_bank_cant_cover_changes_nothing()
        {
            var save = PlayerSave.Load(storage);
            save.Bank(RunThatEarned(100));

            Assert.That(save.Purchase("pistol.magazine", 1, 250), Is.False);

            Assert.That(save.Credits, Is.EqualTo(100));
            Assert.That(save.LevelOf("pistol.magazine"), Is.Zero);
        }

        [Test]
        public void Purchases_survive_an_app_restart()
        {
            var save = PlayerSave.Load(storage);
            save.Bank(RunThatEarned(3000));
            save.Purchase("machine-gun", 1, 2500);
            save.Purchase("pistol.magazine", 1, 250);
            save.Purchase("pistol.magazine", 2, 200);
            save.Write();

            var afterRestart = PlayerSave.Load(storage);

            Assert.That(afterRestart.Credits, Is.EqualTo(50));
            Assert.That(afterRestart.LevelOf("machine-gun"), Is.EqualTo(1));
            Assert.That(afterRestart.LevelOf("pistol.magazine"), Is.EqualTo(2));
        }

        [Test]
        public void A_purchase_needs_an_id_a_level_and_a_price_that_isnt_negative()
        {
            var save = PlayerSave.Load(storage);

            Assert.That(() => save.Purchase(null, 1, 0), Throws.InstanceOf<ArgumentException>());
            Assert.That(() => save.Purchase("x", 0, 0), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => save.Purchase("x", 1, -5), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Credits_can_be_added_outside_a_Run()
        {
            var save = PlayerSave.Load(storage);

            save.AddCredits(1000);
            save.AddCredits(0);
            save.Write();

            Assert.That(save.Credits, Is.EqualTo(1000));
            Assert.That(PlayerSave.Load(storage).Credits, Is.EqualTo(1000));
            Assert.That(() => save.AddCredits(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Reset_forgets_everything()
        {
            var save = PlayerSave.Load(storage);
            save.FinishRun(RunThatEarned(3000));
            save.Purchase("machine-gun", 1, 2500);
            var totals = new List<int>();
            save.CreditsChanged += totals.Add;

            save.Reset();
            save.Write();

            Assert.That(save.Credits, Is.Zero);
            Assert.That(save.RunsFinished, Is.Zero);
            Assert.That(save.LevelOf("machine-gun"), Is.Zero);
            Assert.That(totals, Is.EqualTo(new[] { 0 }));
            var afterRestart = PlayerSave.Load(storage);
            Assert.That(afterRestart.Credits, Is.Zero);
            Assert.That(afterRestart.LevelOf("machine-gun"), Is.Zero);
        }

        [Test]
        public void It_tells_whenever_anything_it_holds_changes()
        {
            var save = PlayerSave.Load(storage);
            var changes = 0;
            save.Changed += () => changes++;

            save.Bank(new RunCredits());
            Assert.That(changes, Is.Zero, "Nothing was banked.");

            save.Bank(RunThatEarned(500));
            Assert.That(changes, Is.EqualTo(1));

            save.Purchase("pistol.magazine", 1, 250);
            Assert.That(changes, Is.EqualTo(2));

            save.Purchase("pistol.magazine", 2, 9999);
            Assert.That(changes, Is.EqualTo(2), "The bank couldn't cover it.");

            save.Reset();
            Assert.That(changes, Is.EqualTo(3));
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
