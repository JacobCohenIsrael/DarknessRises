using System;
using DarknessRises.Game.Health;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Health
{
    public sealed class CharacterHealthTests
    {
        [Test]
        public void Starts_alive_with_full_hit_points()
        {
            var health = new CharacterHealth(5, 1f);

            Assert.That(health.IsAlive, Is.True);
            Assert.That(health.HitPoints, Is.EqualTo(5));
            Assert.That(health.MaxHitPoints, Is.EqualTo(5));
            Assert.That(health.IsInvulnerable, Is.False);
        }

        [Test]
        public void A_landed_hit_takes_hit_points_and_hurts()
        {
            var health = new CharacterHealth(5, 1f);
            var hurts = 0;
            health.Hurt += () => hurts++;

            var landed = health.TakeHit(1);

            Assert.That(landed, Is.True);
            Assert.That(health.HitPoints, Is.EqualTo(4));
            Assert.That(hurts, Is.EqualTo(1));
        }

        [Test]
        public void Hits_during_invulnerability_are_ignored()
        {
            var health = new CharacterHealth(5, 1f);
            health.TakeHit(1);

            health.Tick(0.6f);
            var landed = health.TakeHit(1);

            Assert.That(landed, Is.False);
            Assert.That(health.HitPoints, Is.EqualTo(4));
            Assert.That(health.IsInvulnerable, Is.True);
        }

        [Test]
        public void Invulnerability_wears_off()
        {
            var health = new CharacterHealth(5, 1f);
            health.TakeHit(1);

            health.Tick(0.6f);
            health.Tick(0.4f);

            Assert.That(health.IsInvulnerable, Is.False);
            Assert.That(health.TakeHit(1), Is.True);
            Assert.That(health.HitPoints, Is.EqualTo(3));
        }

        [Test]
        public void Hit_points_never_regenerate()
        {
            var health = new CharacterHealth(5, 1f);
            health.TakeHit(2);

            health.Tick(1000f);

            Assert.That(health.HitPoints, Is.EqualTo(3));
        }

        [Test]
        public void Dies_once_when_hit_points_run_out()
        {
            var health = new CharacterHealth(2, 0f);
            var deaths = 0;
            var hurts = 0;
            health.Died += () => deaths++;
            health.Hurt += () => hurts++;

            health.TakeHit(1);
            health.TakeHit(5);
            var landedAfterDeath = health.TakeHit(1);

            Assert.That(health.IsAlive, Is.False);
            Assert.That(health.HitPoints, Is.Zero);
            Assert.That(deaths, Is.EqualTo(1));
            Assert.That(hurts, Is.EqualTo(1), "The killing hit doesn't also hurt.");
            Assert.That(landedAfterDeath, Is.False);
        }

        [Test]
        public void God_mode_ignores_hits()
        {
            var health = new CharacterHealth(1, 0f) { IsGodMode = true };

            Assert.That(health.TakeHit(1), Is.False);
            Assert.That(health.IsAlive, Is.True);

            health.IsGodMode = false;
            Assert.That(health.TakeHit(1), Is.True);
            Assert.That(health.IsAlive, Is.False);
        }

        [Test]
        public void A_zero_damage_hit_does_not_land()
        {
            var health = new CharacterHealth(3, 1f);

            Assert.That(health.TakeHit(0), Is.False);
            Assert.That(health.IsInvulnerable, Is.False);
        }

        [Test]
        public void Rejects_bad_values()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterHealth(0, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterHealth(5, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterHealth(5, 1f).TakeHit(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterHealth(5, 1f).Tick(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterHealth(5, 1f).Restore(-1f));
        }

        [Test]
        public void Continue_restores_full_hit_points_with_a_moment_of_invulnerability()
        {
            var health = new CharacterHealth(5, 1f);
            health.TakeHit(5);
            var restored = 0;
            health.Restored += () => restored++;

            Assert.That(health.Restore(2.5f), Is.True);

            Assert.That(health.IsAlive, Is.True);
            Assert.That(health.HitPoints, Is.EqualTo(5));
            Assert.That(health.InvulnerableSecondsLeft, Is.EqualTo(2.5f));
            Assert.That(restored, Is.EqualTo(1));
            Assert.That(health.TakeHit(1), Is.False, "Still invulnerable.");

            health.Tick(2.5f);
            Assert.That(health.TakeHit(1), Is.True);
        }

        [Test]
        public void Only_a_dead_character_is_restored()
        {
            var health = new CharacterHealth(5, 1f);
            health.TakeHit(2);
            var restored = 0;
            health.Restored += () => restored++;

            Assert.That(health.Restore(2f), Is.False);
            Assert.That(health.HitPoints, Is.EqualTo(3), "Hit points never regenerate.");
            Assert.That(restored, Is.Zero);
        }

        [Test]
        public void A_restored_character_can_die_again()
        {
            var health = new CharacterHealth(2, 0f);
            var deaths = 0;
            health.Died += () => deaths++;
            health.TakeHit(2);
            health.Restore(0f);

            health.TakeHit(2);

            Assert.That(deaths, Is.EqualTo(2));
            Assert.That(health.IsAlive, Is.False);
        }
    }
}
