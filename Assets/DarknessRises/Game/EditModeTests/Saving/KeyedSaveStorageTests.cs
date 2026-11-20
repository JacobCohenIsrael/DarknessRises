using System;
using DarknessRises.Game.Saving;
using DarknessRises.Game.Storage;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Saving
{
    /// <summary>The save as text under one key of a keyed storage.</summary>
    public sealed class KeyedSaveStorageTests
    {
        private const string Key = "Game.save";

        private MemoryKeyValueStorage keyed;
        private KeyedSaveStorage storage;

        [SetUp]
        public void SetUp()
        {
            keyed = new MemoryKeyValueStorage();
            storage = new KeyedSaveStorage(keyed, Key);
        }

        [Test]
        public void Nothing_is_read_until_something_is_written()
        {
            Assert.That(storage.TryRead(out var text), Is.False);
            Assert.That(text, Is.Null);
        }

        [Test]
        public void What_is_written_is_kept_under_its_key_and_read_back()
        {
            Assert.That(storage.TryWrite("{\"credits\":120}"), Is.True);

            Assert.That(keyed[Key], Is.EqualTo("{\"credits\":120}"));
            Assert.That(storage.TryRead(out var text), Is.True);
            Assert.That(text, Is.EqualTo("{\"credits\":120}"));
        }

        [Test]
        public void It_touches_no_other_key()
        {
            keyed["AnotherGame.save"] = "theirs";

            storage.TryWrite("ours");
            storage.Delete();

            Assert.That(keyed["AnotherGame.save"], Is.EqualTo("theirs"));
            Assert.That(keyed[Key], Is.Null);
        }

        [Test]
        public void A_write_that_fails_leaves_what_was_there()
        {
            storage.TryWrite("older");
            keyed.FailsWrites = true;

            Assert.That(storage.TryWrite("newer"), Is.False);

            Assert.That(storage.TryRead(out var text), Is.True);
            Assert.That(text, Is.EqualTo("older"));
        }

        [Test]
        public void Blocked_storage_gives_nothing_and_takes_nothing()
        {
            storage.TryWrite("kept");
            keyed.IsBlocked = true;

            Assert.That(storage.TryRead(out _), Is.False);
            Assert.That(storage.TryWrite("new"), Is.False);
            Assert.That(() => storage.Delete(), Throws.Nothing);
        }

        [Test]
        public void It_needs_a_storage_and_a_key()
        {
            Assert.That(() => new KeyedSaveStorage(null, Key), Throws.InstanceOf<ArgumentNullException>());
            Assert.That(() => new KeyedSaveStorage(keyed, null), Throws.InstanceOf<ArgumentException>());
            Assert.That(() => new KeyedSaveStorage(keyed, string.Empty), Throws.InstanceOf<ArgumentException>());
        }
    }
}
