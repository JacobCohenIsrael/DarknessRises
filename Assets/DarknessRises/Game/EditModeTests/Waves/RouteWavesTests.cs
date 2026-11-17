using System.Collections.Generic;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Waves;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Waves
{
    public sealed class RouteWavesTests
    {
        private const string Shambler = "Shambler";

        private sealed class FakeSpawner : IEnemySpawner<string>
        {
            public readonly List<Enemy> Alive = new();

            public Enemy TrySpawn(string enemyType)
            {
                var enemy = new Enemy();
                enemy.Spawn(1);
                Alive.Add(enemy);
                return enemy;
            }

            public void KillAll()
            {
                foreach (var enemy in Alive.ToArray())
                {
                    enemy.TakeHit(1);
                }

                Alive.Clear();
            }
        }

        // A 10 m Route walked at 1 m/s, with Stations at 2 m and 5 m.
        private static RouteProgress TwoStationRoute(out Station first, out Station second)
        {
            first = new Station(0, 2f);
            second = new Station(1, 5f);
            var path = new RoutePath(new[] { Vector3.zero, new Vector3(0f, 0f, 10f) });
            return new RouteProgress(path, new[] { first, second }, 1f);
        }

        private static IReadOnlyList<WavePlan<string>> OneShambler()
        {
            return new[] { new WavePlan<string>(0f, 0f, new[] { Shambler }) };
        }

        [Test]
        public void Character_stays_at_a_Station_until_its_Waves_are_dead_then_walks_on()
        {
            var progress = TwoStationRoute(out var first, out _);
            var spawner = new FakeSpawner();
            using var waves = new RouteWaves<string>(progress, new[] { OneShambler(), OneShambler() }, spawner);

            progress.Tick(2f);
            Assert.That(progress.CurrentStation, Is.SameAs(first));

            waves.Tick(0.1f);
            Assert.That(spawner.Alive, Has.Count.EqualTo(1));

            for (var i = 0; i < 10; i++)
            {
                waves.Tick(1f);
                progress.Tick(1f);
            }

            Assert.That(progress.CurrentStation, Is.SameAs(first), "The Wave is still alive.");
            Assert.That(progress.Distance, Is.EqualTo(2f));

            spawner.KillAll();
            waves.Tick(0.1f);
            Assert.That(first.IsCleared, Is.True);
            Assert.That(progress.IsAtStation, Is.False);

            progress.Tick(1f);
            Assert.That(progress.Distance, Is.EqualTo(3f));
        }

        [Test]
        public void Each_Station_fights_its_own_Waves()
        {
            var progress = TwoStationRoute(out var first, out var second);
            var spawner = new FakeSpawner();
            using var waves = new RouteWaves<string>(progress, new[] { OneShambler(), OneShambler() }, spawner);

            progress.Tick(2f);
            waves.Tick(0.1f);
            spawner.KillAll();
            waves.Tick(0.1f);

            progress.Tick(3f);
            Assert.That(progress.CurrentStation, Is.SameAs(second));
            Assert.That(second.IsCleared, Is.False);

            waves.Tick(0.1f);
            Assert.That(spawner.Alive, Has.Count.EqualTo(1));
            spawner.KillAll();
            waves.Tick(0.1f);

            Assert.That(second.IsCleared, Is.True);
            Assert.That(first.IsCleared, Is.True);
        }

        [Test]
        public void Station_without_authored_Waves_is_cleared_on_arrival()
        {
            var progress = TwoStationRoute(out var first, out _);
            using var waves = new RouteWaves<string>(progress, new IReadOnlyList<WavePlan<string>>[0], new FakeSpawner());

            progress.Tick(2f);

            Assert.That(first.IsCleared, Is.True);
            Assert.That(progress.IsAtStation, Is.False);
        }
    }
}
