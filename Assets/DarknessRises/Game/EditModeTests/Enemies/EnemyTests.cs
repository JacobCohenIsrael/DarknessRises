using System;
using DarknessRises.Game.Enemies;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Enemies
{
    public sealed class EnemyTests
    {
        [Test]
        public void Spawned_Enemy_is_alive_with_full_hit_points()
        {
            var enemy = new Enemy();
            enemy.Spawn(4);

            Assert.That(enemy.IsAlive, Is.True);
            Assert.That(enemy.HitPoints, Is.EqualTo(4));
        }

        [Test]
        public void Enemy_dies_once_when_its_hit_points_run_out()
        {
            var enemy = new Enemy();
            enemy.Spawn(3);
            var hurts = 0;
            var deaths = 0;
            enemy.Hurt += _ => hurts++;
            enemy.Died += _ => deaths++;

            enemy.TakeHit(1);
            enemy.TakeHit(1);
            Assert.That(enemy.IsAlive, Is.True);
            Assert.That(hurts, Is.EqualTo(2));

            enemy.TakeHit(5);
            enemy.TakeHit(1);

            Assert.That(enemy.IsAlive, Is.False);
            Assert.That(enemy.HitPoints, Is.Zero);
            Assert.That(deaths, Is.EqualTo(1));
            Assert.That(hurts, Is.EqualTo(2), "A dead Enemy ignores hits.");
        }

        [Test]
        public void Respawned_Enemy_starts_over()
        {
            var enemy = new Enemy();
            enemy.Spawn(1);
            enemy.TakeHit(1);

            enemy.Spawn(2);

            Assert.That(enemy.IsAlive, Is.True);
            Assert.That(enemy.HitPoints, Is.EqualTo(2));
        }

        [Test]
        public void Enemy_needs_at_least_one_hit_point()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Enemy().Spawn(0));
        }
    }
}
