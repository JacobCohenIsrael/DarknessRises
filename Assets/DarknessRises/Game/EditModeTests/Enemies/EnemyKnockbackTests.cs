using System;
using DarknessRises.Game.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Enemies
{
    public sealed class EnemyKnockbackTests
    {
        private const float Tolerance = 1e-4f;
        private const float Cap = 1f;
        private const float SpeedLimit = 8f;

        // The Pistol's push.
        private const float Metres = 0.35f;
        private const float Seconds = 0.12f;

        private static EnemyKnockback Shambler()
        {
            return new EnemyKnockback(1f, Cap, SpeedLimit);
        }

        // Runs the push out in small steps, returning how far it moved and the fastest step.
        private static Vector3 RunOut(EnemyKnockback knockback, float step, out float fastest)
        {
            var moved = Vector3.zero;
            fastest = 0f;
            for (var i = 0; i < 10000 && knockback.IsMoving; i++)
            {
                var delta = knockback.Step(step);
                fastest = Mathf.Max(fastest, delta.magnitude / step);
                moved += delta;
            }

            return moved;
        }

        [Test]
        public void A_hit_pushes_its_distance_along_the_shot_over_its_duration()
        {
            var knockback = Shambler();
            knockback.Push(Vector3.forward, Metres, Seconds);

            var moved = knockback.Step(Seconds * 0.5f) + knockback.Step(Seconds * 0.5f);

            Assert.That(moved.z, Is.EqualTo(Metres).Within(Tolerance));
            Assert.That(moved.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(knockback.IsMoving, Is.False);
        }

        [Test]
        public void The_push_runs_flat_on_the_floor()
        {
            var knockback = Shambler();
            knockback.Push(new Vector3(0f, -3f, 4f), Metres, Seconds);

            var moved = knockback.Step(Seconds);

            Assert.That(moved.y, Is.EqualTo(0f));
            Assert.That(moved.magnitude, Is.EqualTo(Metres).Within(Tolerance));
        }

        [Test]
        public void The_push_eases_out_moving_most_at_the_start()
        {
            var knockback = Shambler();
            knockback.Push(Vector3.forward, Metres, Seconds);

            var firstHalf = knockback.Step(Seconds * 0.5f).magnitude;
            var secondHalf = knockback.Step(Seconds * 0.5f).magnitude;

            Assert.That(firstHalf, Is.EqualTo(Metres * 0.75f).Within(Tolerance));
            Assert.That(secondHalf, Is.EqualTo(Metres * 0.25f).Within(Tolerance));
        }

        [TestCase(0f, 0f)]
        [TestCase(0.5f, 0.75f)]
        [TestCase(1f, 1f)]
        [TestCase(2f, 1f)]
        public void Ease_is_quadratic_ease_out(float progress, float done)
        {
            Assert.That(EnemyKnockback.Ease(progress), Is.EqualTo(done).Within(Tolerance));
        }

        [Test]
        public void No_time_means_no_movement()
        {
            var knockback = Shambler();
            knockback.Push(Vector3.forward, Metres, Seconds);

            Assert.That(knockback.Step(0f), Is.EqualTo(Vector3.zero));
            Assert.That(knockback.Remaining.z, Is.EqualTo(Metres).Within(Tolerance));
        }

        [Test]
        public void The_enemy_type_multiplier_scales_the_distance()
        {
            var runner = new EnemyKnockback(1.3f, Cap, SpeedLimit);
            var heavy = new EnemyKnockback(0.5f, Cap, SpeedLimit);
            runner.Push(Vector3.forward, Metres, Seconds);
            heavy.Push(Vector3.forward, Metres, Seconds);

            Assert.That(RunOut(runner, 0.01f, out _).z, Is.EqualTo(Metres * 1.3f).Within(Tolerance));
            Assert.That(RunOut(heavy, 0.01f, out _).z, Is.EqualTo(Metres * 0.5f).Within(Tolerance));
        }

        [Test]
        public void A_zero_multiplier_never_moves()
        {
            var knockback = new EnemyKnockback(0f, Cap, SpeedLimit);
            knockback.Push(Vector3.forward, Metres, Seconds);

            Assert.That(knockback.IsMoving, Is.False);
            Assert.That(knockback.Step(Seconds), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void A_zero_distance_or_no_direction_pushes_nothing()
        {
            var knockback = Shambler();
            knockback.Push(Vector3.forward, 0f, Seconds);
            knockback.Push(Vector3.up, Metres, Seconds);

            Assert.That(knockback.IsMoving, Is.False);
        }

        [Test]
        public void A_new_hit_adds_to_what_is_left()
        {
            var knockback = Shambler();
            knockback.Push(Vector3.forward, Metres, Seconds);
            var moved = knockback.Step(Seconds * 0.5f);

            knockback.Push(Vector3.forward, Metres, Seconds);
            moved += RunOut(knockback, 0.001f, out _);

            Assert.That(moved.z, Is.EqualTo(Metres * 2f).Within(Tolerance));
        }

        [Test]
        public void Hits_from_different_sides_combine()
        {
            var knockback = Shambler();
            knockback.Push(Vector3.forward, Metres, Seconds);
            knockback.Push(Vector3.back, Metres, Seconds);

            Assert.That(knockback.IsMoving, Is.False);
        }

        [Test]
        public void A_burst_never_pushes_further_than_the_cap_at_once()
        {
            var knockback = Shambler();
            for (var i = 0; i < 50; i++)
            {
                knockback.Push(Vector3.forward, Metres, Seconds);
                Assert.That(knockback.Remaining.magnitude, Is.LessThanOrEqualTo(Cap + Tolerance));
            }

            Assert.That(RunOut(knockback, 0.001f, out _).z, Is.EqualTo(Cap).Within(Tolerance));
        }

        [Test]
        public void A_burst_never_pushes_faster_than_the_speed_limit()
        {
            var knockback = Shambler();
            var fastest = 0f;
            for (var i = 0; i < 50; i++)
            {
                knockback.Push(Vector3.forward, Metres, Seconds);
                fastest = Mathf.Max(fastest, knockback.Speed);
                knockback.Step(0.01f);
            }

            Assert.That(fastest, Is.LessThanOrEqualTo(SpeedLimit + Tolerance));
        }

        [Test]
        public void A_push_that_would_start_too_fast_is_stretched_out()
        {
            var knockback = new EnemyKnockback(1f, Cap, 2f);
            knockback.Push(Vector3.forward, 0.5f, 0.1f);

            // Ease-out starts at twice its average speed: 2 * 0.5 m / 2 m/s.
            Assert.That(knockback.SecondsLeft, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(knockback.Speed, Is.EqualTo(2f).Within(Tolerance));
            RunOut(knockback, 0.001f, out var fastest);
            Assert.That(fastest, Is.LessThanOrEqualTo(2f + 0.01f));
        }

        [Test]
        public void A_new_hit_never_shortens_the_push_under_way()
        {
            var knockback = Shambler();
            knockback.Push(Vector3.forward, Metres, 0.5f);
            knockback.Step(0.1f);

            knockback.Push(Vector3.forward, 0.01f, 0.05f);

            Assert.That(knockback.SecondsLeft, Is.EqualTo(0.4f).Within(Tolerance));
        }

        [Test]
        public void Clear_drops_the_push()
        {
            var knockback = Shambler();
            knockback.Push(Vector3.forward, Metres, Seconds);

            knockback.Clear();

            Assert.That(knockback.IsMoving, Is.False);
            Assert.That(knockback.Remaining, Is.EqualTo(Vector3.zero));
            Assert.That(knockback.Step(Seconds), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void Bad_values_are_refused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyKnockback(-1f, Cap, SpeedLimit));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyKnockback(1f, -1f, SpeedLimit));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyKnockback(1f, Cap, 0f));
            var knockback = Shambler();
            Assert.Throws<ArgumentOutOfRangeException>(() => knockback.Push(Vector3.forward, -1f, Seconds));
            Assert.Throws<ArgumentOutOfRangeException>(() => knockback.Push(Vector3.forward, Metres, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => knockback.Step(-1f));
        }

        [Test]
        public void Knockback_leaves_the_stagger_alone()
        {
            // A hit during the Wind-up both Staggers and pushes: the two rules don't know about each other.
            var enemy = new Enemy();
            enemy.Spawn(4);
            var behaviour = new EnemyBehaviour(enemy, 0.8f, 1, 1.5f, 0.6f);
            behaviour.Begin();
            behaviour.Tick(0.1f, characterInRange: true);
            var knockback = Shambler();

            knockback.Push(Vector3.forward, Metres, Seconds);
            enemy.TakeHit(1);
            knockback.Step(Seconds);

            Assert.That(behaviour.State, Is.EqualTo(EnemyBehaviourState.Staggered));
            Assert.That(knockback.IsMoving, Is.False);
        }
    }
}
