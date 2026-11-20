using System;
using DarknessRises.Game.Saving;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Saving
{
    /// <summary>A save storage that asks for its files to be put on the device after it changes them.</summary>
    public sealed class SyncedSaveStorageTests
    {
        private MemorySaveStorage files;
        private int syncs;
        private SyncedSaveStorage storage;

        [SetUp]
        public void SetUp()
        {
            files = new MemorySaveStorage();
            syncs = 0;
            storage = new SyncedSaveStorage(files, () => syncs++);
        }

        [Test]
        public void A_write_is_followed_by_a_sync()
        {
            Assert.That(storage.TryWrite("save 1"), Is.True);

            Assert.That(files.Text, Is.EqualTo("save 1"));
            Assert.That(syncs, Is.EqualTo(1));
        }

        [Test]
        public void Every_write_that_works_is_synced()
        {
            storage.TryWrite("save 1");
            storage.TryWrite("save 2");

            Assert.That(syncs, Is.EqualTo(2));
        }

        [Test]
        public void A_write_that_fails_has_nothing_to_sync()
        {
            files.FailsWrites = true;

            Assert.That(storage.TryWrite("save 1"), Is.False);

            Assert.That(syncs, Is.Zero);
        }

        [Test]
        public void Reading_syncs_nothing()
        {
            files.Text = "kept";

            Assert.That(storage.TryRead(out var text), Is.True);

            Assert.That(text, Is.EqualTo("kept"));
            Assert.That(syncs, Is.Zero);
        }

        [Test]
        public void A_delete_is_followed_by_a_sync()
        {
            files.Text = "kept";

            storage.Delete();

            Assert.That(files.Text, Is.Null);
            Assert.That(syncs, Is.EqualTo(1));
        }

        [Test]
        public void It_needs_a_storage_and_a_way_to_sync()
        {
            Assert.That(() => new SyncedSaveStorage(null, () => { }), Throws.InstanceOf<ArgumentNullException>());
            Assert.That(() => new SyncedSaveStorage(files, null), Throws.InstanceOf<ArgumentNullException>());
        }
    }
}
