using DarknessRises.Game.Saving;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Saving
{
    public sealed class SaveRecordTests
    {
        [Test]
        public void A_record_reads_back_as_it_was_written()
        {
            var text = SaveFormat.ToText(new SaveRecord { credits = 1234, runsFinished = 7 });

            Assert.That(SaveFormat.TryParse(text, out var record), Is.True);
            Assert.That(record.credits, Is.EqualTo(1234));
            Assert.That(record.runsFinished, Is.EqualTo(7));
        }

        [Test]
        public void Writing_stamps_the_version_this_build_writes()
        {
            var text = SaveFormat.ToText(new SaveRecord { version = 0, credits = 5 });

            SaveFormat.TryParse(text, out var record);

            Assert.That(record.version, Is.EqualTo(SaveRecord.CurrentVersion));
            Assert.That(text, Does.Contain("\"version\""));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("{ \"credits\": 12")]
        [TestCase("not json at all")]
        public void Text_that_isnt_a_record_doesnt_parse(string text)
        {
            Assert.That(SaveFormat.TryParse(text, out var record), Is.False);
            Assert.That(record, Is.Null);
        }

        [Test]
        public void Text_without_a_version_migrates_to_the_first_version_with_what_it_holds()
        {
            SaveFormat.TryParse("{\"credits\":300,\"runsFinished\":2}", out var record);
            Assert.That(record.version, Is.Zero, "No version in the text.");

            SaveMigration.ToCurrent(record);

            Assert.That(record.version, Is.EqualTo(SaveRecord.CurrentVersion));
            Assert.That(record.credits, Is.EqualTo(300));
            Assert.That(record.runsFinished, Is.EqualTo(2));
        }

        [Test]
        public void A_record_of_this_version_migrates_to_itself()
        {
            var record = new SaveRecord { version = SaveRecord.CurrentVersion, credits = 90, runsFinished = 4 };

            SaveMigration.ToCurrent(record);

            Assert.That(record.version, Is.EqualTo(SaveRecord.CurrentVersion));
            Assert.That(record.credits, Is.EqualTo(90));
            Assert.That(record.runsFinished, Is.EqualTo(4));
        }

        [Test]
        public void A_record_from_a_newer_build_is_read_as_far_as_this_build_understands_it()
        {
            var text = "{\"version\":99,\"credits\":450,\"runsFinished\":12,\"somethingNew\":{\"x\":1}}";
            SaveFormat.TryParse(text, out var record);

            SaveMigration.ToCurrent(record);

            Assert.That(record.version, Is.EqualTo(SaveRecord.CurrentVersion));
            Assert.That(record.credits, Is.EqualTo(450));
            Assert.That(record.runsFinished, Is.EqualTo(12));
        }

        [Test]
        public void Values_no_version_allows_are_put_right()
        {
            var record = new SaveRecord { version = 1, credits = -50, runsFinished = -1 };

            SaveMigration.ToCurrent(record);

            Assert.That(record.credits, Is.Zero);
            Assert.That(record.runsFinished, Is.Zero);
        }

        [Test]
        public void An_older_save_loads_through_the_migration()
        {
            var storage = new MemorySaveStorage { Text = "{\"credits\":300,\"runsFinished\":2}" };

            var save = PlayerSave.Load(storage);

            Assert.That(save.Credits, Is.EqualTo(300));
            Assert.That(save.RunsFinished, Is.EqualTo(2));
        }
    }
}
