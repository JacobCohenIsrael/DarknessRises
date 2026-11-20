using DarknessRises.Game.Runs;
using DarknessRises.Game.Saving;
using DarknessRises.Game.Storage;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Saving
{
    /// <summary>
    /// Where the save is kept on each platform, and what that means for a player: on the Web, progress follows the
    /// game from one upload to the next, although each upload's own files start empty.
    /// </summary>
    public sealed class SaveStoragesTests
    {
        // The browser's storage: one for the site, shared by every upload of the game.
        private MemoryKeyValueStorage browser;

        // How many times the game asked for its files to be put in the browser.
        private int fileSyncs;

        [SetUp]
        public void SetUp()
        {
            browser = new MemoryKeyValueStorage();
            fileSyncs = 0;
        }

        private void SyncFiles() => fileSyncs++;

        private static RunCredits RunThatEarned(int credits)
        {
            var runCredits = new RunCredits();
            runCredits.Earn(credits);
            return runCredits;
        }

        // An upload of the game opened in the browser: it has files of its own, kept under its own address.
        private PlayerSave Launch(MemorySaveStorage uploadsOwnFiles)
        {
            return PlayerSave.Load(SaveStorages.Choose(browser, uploadsOwnFiles, SyncFiles));
        }

        [Test]
        public void Off_the_Web_the_save_is_the_file_and_nothing_else()
        {
            var file = new MemorySaveStorage();

            Assert.That(SaveStorages.Choose(null, file, SyncFiles), Is.SameAs(file));
            Assert.That(SaveStorages.Choose(null, file, null), Is.SameAs(file), "There are no files to sync off the Web.");
        }

        [Test]
        public void On_the_Web_the_save_is_kept_in_the_browsers_storage_under_the_games_own_key()
        {
            var files = new MemorySaveStorage();
            var save = Launch(files);

            save.Bank(RunThatEarned(120));

            Assert.That(browser[BrowserKeys.Save], Does.Contain("\"credits\":120"));
            Assert.That(browser.Count, Is.EqualTo(1));
            Assert.That(files.Text, Is.Null, "The upload's own files aren't needed.");
            Assert.That(fileSyncs, Is.Zero);
        }

        [Test]
        public void A_new_upload_finds_the_Credits_and_purchases_of_the_last_one()
        {
            var firstUpload = Launch(new MemorySaveStorage());
            firstUpload.FinishRun(RunThatEarned(3000));
            firstUpload.Purchase("machine-gun", 1, 2500);
            firstUpload.Write();

            // The next version is served from a new address: its own files are empty.
            var secondUpload = Launch(new MemorySaveStorage());

            Assert.That(secondUpload.Credits, Is.EqualTo(500));
            Assert.That(secondUpload.LevelOf("machine-gun"), Is.EqualTo(1));
            Assert.That(secondUpload.RunsFinished, Is.EqualTo(1));
        }

        [Test]
        public void Without_the_browsers_storage_a_new_upload_starts_from_nothing()
        {
            // What the game did before: every upload read only its own files.
            var firstUpload = PlayerSave.Load(SaveStorages.Choose(null, new MemorySaveStorage(), SyncFiles));
            firstUpload.Bank(RunThatEarned(3000));

            var secondUpload = PlayerSave.Load(SaveStorages.Choose(null, new MemorySaveStorage(), SyncFiles));

            Assert.That(secondUpload.Credits, Is.Zero);
        }

        [Test]
        public void A_save_in_this_uploads_own_files_is_carried_over_at_the_first_launch()
        {
            // An earlier build, played at this same address, left its save in the files.
            var files = new MemorySaveStorage();
            PlayerSave.Load(files).Bank(RunThatEarned(750));

            var save = Launch(files);

            Assert.That(save.Credits, Is.EqualTo(750));
            Assert.That(browser[BrowserKeys.Save], Does.Contain("\"credits\":750"), "Carried over on load, before the save changes.");

            // And from there it follows the game to the next upload.
            Assert.That(Launch(new MemorySaveStorage()).Credits, Is.EqualTo(750));
        }

        [Test]
        public void With_the_browsers_storage_blocked_the_game_saves_to_its_own_files_as_before()
        {
            browser.IsBlocked = true;
            var files = new MemorySaveStorage();
            var save = Launch(files);

            save.Bank(RunThatEarned(200));

            Assert.That(save.Credits, Is.EqualTo(200));
            Assert.That(files.Text, Does.Contain("\"credits\":200"));
            Assert.That(fileSyncs, Is.EqualTo(1), "A save written to the files is put in the browser at once.");
            Assert.That(Launch(files).Credits, Is.EqualTo(200), "The same upload still finds it.");
        }

        [Test]
        public void With_storage_blocked_everywhere_the_Credits_stay_in_memory_and_are_written_once_storage_works()
        {
            browser.IsBlocked = true;
            var files = new MemorySaveStorage { FailsWrites = true };
            var save = Launch(files);

            save.Bank(RunThatEarned(200));
            Assert.That(save.Credits, Is.EqualTo(200), "Nothing in memory is lost.");
            Assert.That(save.Write(), Is.False);
            Assert.That(fileSyncs, Is.Zero, "Nothing was written, so there is nothing to sync.");

            browser.IsBlocked = false;
            Assert.That(save.Write(), Is.True);
            Assert.That(browser[BrowserKeys.Save], Does.Contain("\"credits\":200"));
        }

        [Test]
        public void With_the_browsers_storage_full_the_newer_save_is_the_one_found_next_time()
        {
            var files = new MemorySaveStorage();
            var save = Launch(files);
            save.Bank(RunThatEarned(100));

            browser.FailsWrites = true;
            save.Bank(RunThatEarned(50));

            browser.FailsWrites = false;
            Assert.That(Launch(files).Credits, Is.EqualTo(150));
        }

        [Test]
        public void Text_that_isnt_a_save_is_no_save_as_with_a_corrupt_file()
        {
            browser[BrowserKeys.Save] = "{ \"credits\": 12";

            var save = Launch(new MemorySaveStorage());

            Assert.That(save.Credits, Is.Zero);
            save.Bank(RunThatEarned(40));
            Assert.That(browser[BrowserKeys.Save], Does.Contain("\"credits\":40"), "The next save replaces it.");
        }

        [Test]
        public void An_older_records_version_is_migrated_from_the_browsers_storage_like_from_a_file()
        {
            browser[BrowserKeys.Save] = "{\"version\":1,\"credits\":375,\"runsFinished\":3}";

            var save = Launch(new MemorySaveStorage());

            Assert.That(save.Credits, Is.EqualTo(375));
            Assert.That(save.RunsFinished, Is.EqualTo(3));
        }

        [Test]
        public void On_the_Web_the_selection_needs_the_files_and_their_sync()
        {
            Assert.That(() => SaveStorages.Choose(browser, null, SyncFiles), Throws.InstanceOf<System.ArgumentNullException>());
            Assert.That(() => SaveStorages.Choose(browser, new MemorySaveStorage(), null), Throws.InstanceOf<System.ArgumentNullException>());
        }

        [Test]
        public void Another_games_keys_in_the_same_browser_storage_are_never_touched()
        {
            browser["save"] = "another game's";
            browser["SomeGame.save.v1"] = "and another's";
            var save = Launch(new MemorySaveStorage());

            save.Bank(RunThatEarned(10));
            save.Reset();
            save.Write();

            Assert.That(browser["save"], Is.EqualTo("another game's"));
            Assert.That(browser["SomeGame.save.v1"], Is.EqualTo("and another's"));
        }
    }
}
