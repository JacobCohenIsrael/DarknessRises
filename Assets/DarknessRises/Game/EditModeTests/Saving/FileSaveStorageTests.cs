using System;
using System.IO;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Saving;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Saving
{
    /// <summary>
    /// The file adapter, against real files in a folder of its own under the project's Temp folder (never the player's
    /// persistent data path), removed after each test.
    /// </summary>
    public sealed class FileSaveStorageTests
    {
        private string folder;
        private string path;

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "SaveTests", Guid.NewGuid().ToString("N"));
            path = Path.Combine(folder, FileSaveStorage.FileName);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }

        [Test]
        public void Nothing_is_read_before_anything_is_written()
        {
            var storage = new FileSaveStorage(path);

            Assert.That(storage.TryRead(out var text), Is.False);
            Assert.That(text, Is.Null);
            Assert.That(Directory.Exists(folder), Is.False, "Reading creates nothing.");
        }

        [Test]
        public void What_is_written_is_read_back()
        {
            var storage = new FileSaveStorage(path);

            Assert.That(storage.TryWrite("first"), Is.True);

            Assert.That(storage.TryRead(out var text), Is.True);
            Assert.That(text, Is.EqualTo("first"));
        }

        [Test]
        public void A_write_replaces_the_save_whole_and_leaves_no_temporary_file()
        {
            var storage = new FileSaveStorage(path);
            storage.TryWrite("first");

            storage.TryWrite("second");

            storage.TryRead(out var text);
            Assert.That(text, Is.EqualTo("second"));
            Assert.That(File.ReadAllText(path), Is.EqualTo("second"));
            Assert.That(File.Exists(path + ".new"), Is.False);
        }

        [Test]
        public void The_save_it_replaced_is_kept_as_a_backup_and_read_when_the_save_is_missing()
        {
            var storage = new FileSaveStorage(path);
            storage.TryWrite("first");
            storage.TryWrite("second");

            File.Delete(path);

            Assert.That(storage.TryRead(out var text), Is.True);
            Assert.That(text, Is.EqualTo("first"));
        }

        [Test]
        public void Delete_removes_the_save_and_its_backup()
        {
            var storage = new FileSaveStorage(path);
            storage.TryWrite("first");
            storage.TryWrite("second");

            storage.Delete();

            Assert.That(storage.TryRead(out _), Is.False);
            Assert.That(Directory.GetFiles(folder), Is.Empty);
        }

        [Test]
        public void The_bank_survives_an_app_restart_on_disk()
        {
            var credits = new RunCredits();
            credits.Earn(275);
            PlayerSave.Load(new FileSaveStorage(path)).FinishRun(credits);

            var afterRestart = PlayerSave.Load(new FileSaveStorage(path));

            Assert.That(afterRestart.Credits, Is.EqualTo(275));
            Assert.That(afterRestart.RunsFinished, Is.EqualTo(1));
        }

        [Test]
        public void It_needs_a_path()
        {
            Assert.That(() => new FileSaveStorage(null), Throws.InstanceOf<ArgumentException>());
            Assert.That(() => new FileSaveStorage(string.Empty), Throws.InstanceOf<ArgumentException>());
        }
    }
}
