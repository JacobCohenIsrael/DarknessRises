using System.Collections.Generic;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Waves;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Waves
{
    public sealed class WaveScheduleTests
    {
        private const string Shambler = "Shambler";
        private const string Runner = "Runner";

        /// <summary>Spawns Enemies on request unless told every Spawn point is lit, and remembers what it spawned.</summary>
        private sealed class FakeSpawner : IEnemySpawner<string>
        {
            public readonly List<string> Spawned = new();
            public readonly List<Enemy> Alive = new();
            public bool AllSpawnPointsLit;

            public Enemy TrySpawn(string enemyType)
            {
                if (AllSpawnPointsLit)
                {
                    return null;
                }

                var enemy = new Enemy();
                enemy.Spawn(1);
                Spawned.Add(enemyType);
                Alive.Add(enemy);
                return enemy;
            }

            public void KillAll()
            {
                var killing = Alive.ToArray();
                Alive.Clear();
                foreach (var enemy in killing)
                {
                    enemy.TakeHit(1);
                }
            }
        }

        private static WavePlan<string> Wave(float delaySeconds, float intervalSeconds, params string[] enemies)
        {
            return new WavePlan<string>(delaySeconds, intervalSeconds, enemies);
        }

        private static WaveSchedule<string> Begin(FakeSpawner spawner, params WavePlan<string>[] waves)
        {
            var schedule = new WaveSchedule<string>(waves, spawner);
            schedule.Begin();
            return schedule;
        }

        [Test]
        public void First_Enemy_enters_once_the_Wave_delay_is_over()
        {
            var spawner = new FakeSpawner();
            Begin(spawner, Wave(2f, 1f, Shambler)).Tick(1.9f);
            Assert.That(spawner.Spawned, Is.Empty);

            var schedule = Begin(spawner, Wave(2f, 1f, Shambler));
            schedule.Tick(1f);
            schedule.Tick(1f);
            Assert.That(spawner.Spawned, Is.EqualTo(new[] { Shambler }));
        }

        [Test]
        public void Enemies_enter_one_spawn_interval_apart_in_authored_order()
        {
            var spawner = new FakeSpawner();
            var schedule = Begin(spawner, Wave(0f, 1f, Shambler, Runner, Shambler));

            schedule.Tick(0.1f);
            Assert.That(spawner.Spawned, Is.EqualTo(new[] { Shambler }));

            schedule.Tick(0.75f);
            Assert.That(spawner.Spawned, Has.Count.EqualTo(1));

            schedule.Tick(0.25f);
            Assert.That(spawner.Spawned, Is.EqualTo(new[] { Shambler, Runner }));

            schedule.Tick(1f);
            Assert.That(spawner.Spawned, Is.EqualTo(new[] { Shambler, Runner, Shambler }));
        }

        [Test]
        public void A_long_tick_lets_in_every_Enemy_that_was_due()
        {
            var spawner = new FakeSpawner();
            var schedule = Begin(spawner, Wave(0f, 0.5f, Shambler, Shambler, Shambler, Shambler));
            schedule.Tick(0.1f);

            schedule.Tick(1.1f);

            Assert.That(spawner.Spawned, Has.Count.EqualTo(3));
        }

        [Test]
        public void Next_Wave_waits_for_the_last_Enemy_of_the_previous_one_to_die_then_its_delay()
        {
            var spawner = new FakeSpawner();
            var schedule = Begin(spawner, Wave(0f, 0f, Shambler, Shambler), Wave(1f, 0f, Runner));
            schedule.Tick(0.1f);
            Assert.That(spawner.Spawned, Has.Count.EqualTo(2));

            schedule.Tick(10f);
            Assert.That(spawner.Spawned, Has.Count.EqualTo(2), "The second Wave must not start while the first is alive.");

            spawner.Alive[0].TakeHit(1);
            schedule.Tick(10f);
            Assert.That(spawner.Spawned, Has.Count.EqualTo(2), "One Shambler of the first Wave is still alive.");

            spawner.KillAll();
            schedule.Tick(0.1f);
            Assert.That(schedule.WaveIndex, Is.EqualTo(1));
            schedule.Tick(0.5f);
            Assert.That(spawner.Spawned, Has.Count.EqualTo(2), "The second Wave's delay isn't over yet.");

            schedule.Tick(0.5f);
            Assert.That(spawner.Spawned, Is.EqualTo(new[] { Shambler, Shambler, Runner }));
        }

        [Test]
        public void Schedule_is_done_once_the_last_Wave_is_dead()
        {
            var spawner = new FakeSpawner();
            var schedule = Begin(spawner, Wave(0f, 0f, Shambler), Wave(0f, 0f, Shambler, Shambler));

            schedule.Tick(0.1f);
            spawner.KillAll();
            schedule.Tick(0.1f);
            schedule.Tick(0.1f);
            Assert.That(spawner.Spawned, Has.Count.EqualTo(3));
            Assert.That(schedule.IsDone, Is.False, "The last Wave is still alive.");

            spawner.KillAll();
            schedule.Tick(0.1f);

            Assert.That(schedule.IsDone, Is.True);
            Assert.That(schedule.AliveCount, Is.Zero);
        }

        [Test]
        public void Schedule_is_not_done_while_its_Wave_is_still_entering()
        {
            var spawner = new FakeSpawner();
            var schedule = Begin(spawner, Wave(0f, 5f, Shambler, Shambler));

            schedule.Tick(0.1f);
            spawner.KillAll();
            schedule.Tick(1f);

            Assert.That(schedule.IsDone, Is.False);
        }

        [Test]
        public void Schedule_without_Waves_is_done_at_once()
        {
            var spawner = new FakeSpawner();
            var schedule = Begin(spawner);

            Assert.That(schedule.IsDone, Is.True);
        }

        [Test]
        public void An_Enemy_that_finds_every_Spawn_point_lit_waits_and_enters_once_one_is_dark()
        {
            var spawner = new FakeSpawner { AllSpawnPointsLit = true };
            var schedule = Begin(spawner, Wave(0f, 1f, Shambler, Runner));

            schedule.Tick(0.1f);
            schedule.Tick(3f);
            Assert.That(spawner.Spawned, Is.Empty);

            spawner.AllSpawnPointsLit = false;
            schedule.Tick(0.1f);
            Assert.That(spawner.Spawned, Is.EqualTo(new[] { Shambler }), "Time spent waiting isn't banked as faster spawns.");

            schedule.Tick(1f);
            Assert.That(spawner.Spawned, Is.EqualTo(new[] { Shambler, Runner }));
        }

        [Test]
        public void Waves_start_one_after_another()
        {
            var spawner = new FakeSpawner();
            var schedule = Begin(spawner, Wave(0f, 0f, Shambler), Wave(0f, 0f, Runner));
            var started = new List<int>();
            schedule.WaveStarted += started.Add;

            schedule.Tick(0.1f);
            spawner.KillAll();
            schedule.Tick(0.1f);
            schedule.Tick(0.1f);

            Assert.That(started, Is.EqualTo(new[] { 0, 1 }));
        }
    }
}
