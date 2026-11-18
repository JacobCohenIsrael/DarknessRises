using DarknessRises.Game.Runs;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Runs
{
    public sealed class RunRecordTests
    {
        [Test]
        public void A_new_Run_is_on_the_first_Level_with_no_kills()
        {
            var record = new RunRecord();

            Assert.That(record.LevelReached, Is.EqualTo(RunRecord.FirstLevel));
            Assert.That(record.EnemiesKilled, Is.Zero);
        }

        [Test]
        public void Counts_Enemies_killed()
        {
            var record = new RunRecord();

            record.CountKill();
            record.CountKill();

            Assert.That(record.EnemiesKilled, Is.EqualTo(2));
        }

        [Test]
        public void Records_the_Level_reached()
        {
            var record = new RunRecord();

            record.Reach(4);

            Assert.That(record.LevelReached, Is.EqualTo(4));
            Assert.That(() => record.Reach(0), Throws.InstanceOf<System.ArgumentOutOfRangeException>());
        }
    }
}
