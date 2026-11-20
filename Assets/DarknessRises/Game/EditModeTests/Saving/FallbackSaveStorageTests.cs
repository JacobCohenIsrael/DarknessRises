using System;
using DarknessRises.Game.Saving;
using DarknessRises.Game.Storage;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Saving
{
    /// <summary>
    /// The save kept in a first place (the browser's storage) with a second to fall back on (the player's own file):
    /// where it's read from, where it's written, and what happens when the first place can't be used.
    /// </summary>
    public sealed class FallbackSaveStorageTests
    {
        private const string Key = "Game.save";

        private MemoryKeyValueStorage browser;
        private MemorySaveStorage file;
        private FallbackSaveStorage storage;

        [SetUp]
        public void SetUp()
        {
            browser = new MemoryKeyValueStorage();
            file = new MemorySaveStorage();
            storage = NewStorage();
        }

        // The storage as the next launch makes it, over the same two places.
        private FallbackSaveStorage NewStorage() => new(new KeyedSaveStorage(browser, Key), file);

        [Test]
        public void With_nothing_anywhere_there_is_no_save_and_nothing_is_written()
        {
            Assert.That(storage.TryRead(out var text), Is.False);

            Assert.That(text, Is.Null);
            Assert.That(browser.Count, Is.Zero);
            Assert.That(file.WriteCount, Is.Zero);
        }

        [Test]
        public void The_save_is_written_to_the_first_place_only()
        {
            Assert.That(storage.TryWrite("save 1"), Is.True);

            Assert.That(browser[Key], Is.EqualTo("save 1"));
            Assert.That(file.Text, Is.Null);
            Assert.That(storage.IsOnFallback, Is.False);
        }

        [Test]
        public void The_save_is_read_from_the_first_place_when_it_has_one()
        {
            browser[Key] = "newer";
            file.Text = "older";

            Assert.That(storage.TryRead(out var text), Is.True);

            Assert.That(text, Is.EqualTo("newer"));
            Assert.That(file.Text, Is.EqualTo("older"), "The fallback is left alone.");
        }

        [Test]
        public void A_save_only_the_fallback_has_is_read_and_carried_over_to_the_first_place()
        {
            file.Text = "from the old build";

            Assert.That(storage.TryRead(out var text), Is.True);

            Assert.That(text, Is.EqualTo("from the old build"));
            Assert.That(browser[Key], Is.EqualTo("from the old build"), "Carried over at once, before anything changes the save.");
            Assert.That(file.Text, Is.EqualTo("from the old build"), "The fallback keeps its copy.");
            Assert.That(storage.IsOnFallback, Is.False);
        }

        [Test]
        public void After_the_carry_over_the_first_place_is_the_one_written_and_read()
        {
            file.Text = "from the old build";
            storage.TryRead(out _);

            storage.TryWrite("played on");

            Assert.That(browser[Key], Is.EqualTo("played on"));
            Assert.That(file.Text, Is.EqualTo("from the old build"));
            Assert.That(NewStorage().TryRead(out var text), Is.True);
            Assert.That(text, Is.EqualTo("played on"));
        }

        [Test]
        public void When_the_first_place_cant_take_the_save_it_goes_to_the_fallback()
        {
            storage.TryWrite("older");
            browser.FailsWrites = true;

            Assert.That(storage.TryWrite("newer"), Is.True);

            Assert.That(file.Text, Is.EqualTo("newer"));
            Assert.That(storage.IsOnFallback, Is.True);
        }

        [Test]
        public void The_first_places_older_save_is_removed_so_the_newer_one_is_read_next_time()
        {
            storage.TryWrite("older");
            browser.FailsWrites = true;
            storage.TryWrite("newer");

            Assert.That(browser[Key], Is.Null);
            Assert.That(NewStorage().TryRead(out var text), Is.True);
            Assert.That(text, Is.EqualTo("newer"));
        }

        [Test]
        public void When_neither_place_takes_the_save_nothing_stored_is_lost()
        {
            storage.TryWrite("older");
            browser.FailsWrites = true;
            file.FailsWrites = true;

            Assert.That(storage.TryWrite("newer"), Is.False);

            Assert.That(browser[Key], Is.EqualTo("older"), "An older save is better than none.");
            Assert.That(file.Text, Is.Null);
        }

        [Test]
        public void Once_the_first_place_works_again_the_save_goes_back_to_it()
        {
            browser.FailsWrites = true;
            storage.TryWrite("while it was full");
            browser.FailsWrites = false;

            Assert.That(storage.TryWrite("after"), Is.True);

            Assert.That(browser[Key], Is.EqualTo("after"));
            Assert.That(storage.IsOnFallback, Is.False);
            Assert.That(NewStorage().TryRead(out var text), Is.True);
            Assert.That(text, Is.EqualTo("after"));
        }

        [Test]
        public void A_save_left_on_the_fallback_is_carried_over_at_the_next_launch_that_can()
        {
            browser.FailsWrites = true;
            storage.TryWrite("while it was full");
            browser.FailsWrites = false;

            Assert.That(NewStorage().TryRead(out var text), Is.True);

            Assert.That(text, Is.EqualTo("while it was full"));
            Assert.That(browser[Key], Is.EqualTo("while it was full"));
        }

        [Test]
        public void With_the_first_place_blocked_it_works_like_the_fallback_alone()
        {
            browser.IsBlocked = true;

            Assert.That(storage.TryRead(out _), Is.False);
            Assert.That(storage.TryWrite("save 1"), Is.True);
            Assert.That(storage.TryWrite("save 2"), Is.True);

            Assert.That(file.Text, Is.EqualTo("save 2"));
            Assert.That(browser.Count, Is.Zero);
            Assert.That(NewStorage().TryRead(out var text), Is.True);
            Assert.That(text, Is.EqualTo("save 2"));
        }

        [Test]
        public void Reading_from_the_fallback_while_the_first_place_is_blocked_still_gives_the_save_every_launch()
        {
            file.Text = "kept in the file";
            browser.IsBlocked = true;

            for (var launch = 0; launch < 3; launch++)
            {
                var launched = NewStorage();
                Assert.That(launched.TryRead(out var text), Is.True);
                Assert.That(text, Is.EqualTo("kept in the file"));
                Assert.That(launched.IsOnFallback, Is.True);
            }
        }

        [Test]
        public void Deleting_removes_the_save_from_both_places()
        {
            browser[Key] = "one";
            file.Text = "two";

            storage.Delete();

            Assert.That(browser[Key], Is.Null);
            Assert.That(file.Text, Is.Null);
            Assert.That(storage.TryRead(out _), Is.False);
        }

        [Test]
        public void It_needs_both_places()
        {
            Assert.That(() => new FallbackSaveStorage(null, file), Throws.InstanceOf<ArgumentNullException>());
            Assert.That(() => new FallbackSaveStorage(file, null), Throws.InstanceOf<ArgumentNullException>());
        }
    }
}
