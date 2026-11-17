using DarknessRises.Game.Levels;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Levels
{
    public sealed class StragglerScheduleTests
    {
        private const string Shambler = "Shambler";

        private FakeSpawner spawner;
        private ScriptedRandom random;
        private StragglerSchedule<string> schedule;

        [SetUp]
        public void SetUp()
        {
            spawner = new FakeSpawner();
            random = new ScriptedRandom();
            schedule = new StragglerSchedule<string>(spawner, random);
        }

        private static StragglerPlan<string> Plan(int maxAlive = 1) => new(Shambler, 4f, 8f, maxAlive);

        [Test]
        public void A_Straggler_comes_a_random_interval_from_the_Rooms_range_after_walking_in()
        {
            random.Next(5f);
            schedule.Follow(Plan());

            Assert.That(random.LastRange, Is.EqualTo((4f, 8f)));
            schedule.Tick(4.9f, walking: true);
            Assert.That(spawner.Spawned, Is.Empty);

            schedule.Tick(0.1f, walking: true);
            Assert.That(spawner.Spawned, Is.EqualTo(new[] { Shambler }));
            Assert.That(schedule.AliveCount, Is.EqualTo(1));
        }

        [Test]
        public void The_clock_stops_while_the_character_is_not_walking()
        {
            random.Next(5f);
            schedule.Follow(Plan());

            schedule.Tick(100f, walking: false);
            Assert.That(spawner.Spawned, Is.Empty);
            Assert.That(schedule.SecondsUntilNext, Is.EqualTo(5f));

            schedule.Tick(5f, walking: true);
            Assert.That(spawner.Spawned, Has.Count.EqualTo(1));
        }

        [Test]
        public void No_more_come_while_as_many_as_the_Room_allows_are_alive()
        {
            random.Next(4f, 4f, 4f);
            schedule.Follow(Plan(maxAlive: 1));

            schedule.Tick(4f, walking: true);
            schedule.Tick(4f, walking: true);
            Assert.That(spawner.Spawned, Has.Count.EqualTo(1), "The first Straggler is still alive.");

            spawner.KillAll();
            Assert.That(schedule.AliveCount, Is.EqualTo(0));
            schedule.Tick(4f, walking: true);
            Assert.That(spawner.Spawned, Has.Count.EqualTo(2));
        }

        [Test]
        public void A_Straggler_that_cant_enter_tries_again_every_tick_without_banking_time()
        {
            random.Next(4f, 6f);
            schedule.Follow(Plan(maxAlive: 3));
            spawner.Closed = true;

            schedule.Tick(10f, walking: true);
            Assert.That(spawner.Spawned, Is.Empty);

            spawner.Closed = false;
            schedule.Tick(0.01f, walking: true);
            Assert.That(spawner.Spawned, Has.Count.EqualTo(1));
            Assert.That(schedule.SecondsUntilNext, Is.EqualTo(6f), "The next one waits a full interval.");
        }

        [Test]
        public void A_Room_without_Stragglers_sends_none_and_keeps_those_already_alive()
        {
            random.Next(4f);
            schedule.Follow(Plan());
            schedule.Tick(4f, walking: true);

            schedule.Follow(null);
            schedule.Tick(1000f, walking: true);

            Assert.That(spawner.Spawned, Has.Count.EqualTo(1));
            Assert.That(schedule.AliveCount, Is.EqualTo(1));
            Assert.That(schedule.SecondsUntilNext, Is.EqualTo(float.PositiveInfinity));
        }
    }
}
