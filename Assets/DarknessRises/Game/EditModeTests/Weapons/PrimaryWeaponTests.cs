using System;
using DarknessRises.Game.Weapons;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Weapons
{
    public sealed class PrimaryWeaponTests
    {
        // Pistol numbers. 4 shots per second is a gap of exactly 0.25 s, which floats represent exactly.
        private const float ShotsPerSecond = 4f;
        private const float Gap = 0.25f;
        private const int MagazineSize = 12;
        private const float ReloadSeconds = 1.5f;

        private PrimaryWeapon weapon;
        private int shotsFired;
        private int reloadsStarted;
        private int reloadsFinished;

        [SetUp]
        public void SetUp()
        {
            weapon = new PrimaryWeapon(new PrimaryWeaponStats(ShotsPerSecond, MagazineSize, ReloadSeconds));
            shotsFired = 0;
            reloadsStarted = 0;
            reloadsFinished = 0;
            weapon.Fired += () => shotsFired++;
            weapon.ReloadStarted += () => reloadsStarted++;
            weapon.ReloadFinished += () => reloadsFinished++;
        }

        [Test]
        public void StartsWithAFullMagazine()
        {
            Assert.That(weapon.RoundsInMagazine, Is.EqualTo(MagazineSize));
            Assert.That(weapon.MagazineSize, Is.EqualTo(MagazineSize));
            Assert.That(weapon.IsReloading, Is.False);
        }

        [Test]
        public void DoesNotFireWithoutTheTrigger()
        {
            weapon.Tick(5f);

            Assert.That(shotsFired, Is.EqualTo(0));
            Assert.That(weapon.RoundsInMagazine, Is.EqualTo(MagazineSize));
        }

        [Test]
        public void PullingTheTriggerFiresAtOnce()
        {
            weapon.PullTrigger();
            weapon.Tick(0f);

            Assert.That(shotsFired, Is.EqualTo(1));
            Assert.That(weapon.RoundsInMagazine, Is.EqualTo(MagazineSize - 1));
        }

        [Test]
        public void HoldingTheTriggerFiresAtTheFireRate()
        {
            weapon.PullTrigger();
            weapon.Tick(0f);

            weapon.Tick(Gap * 0.5f);
            Assert.That(shotsFired, Is.EqualTo(1), "Half a gap after the first shot.");

            weapon.Tick(Gap * 0.5f);
            Assert.That(shotsFired, Is.EqualTo(2), "One gap after the first shot.");

            weapon.Tick(Gap);
            weapon.Tick(Gap);
            Assert.That(shotsFired, Is.EqualTo(4));
        }

        [Test]
        public void ALongFrameFiresEveryShotItCovers()
        {
            weapon.PullTrigger();
            weapon.Tick(Gap * 3f);

            // Shots at 0, 1, 2 and 3 gaps.
            Assert.That(shotsFired, Is.EqualTo(4));
        }

        [Test]
        public void TappingFasterThanTheFireRateIsGated()
        {
            weapon.PullTrigger();
            weapon.Tick(0f);
            weapon.ReleaseTrigger();
            weapon.Tick(Gap * 0.5f);

            weapon.PullTrigger();
            weapon.Tick(0f);
            Assert.That(shotsFired, Is.EqualTo(1), "Too soon after the last shot.");

            weapon.Tick(Gap * 0.5f);
            Assert.That(shotsFired, Is.EqualTo(2), "Fires as soon as the gap has passed.");
        }

        [Test]
        public void ReleasingTheTriggerNeverBanksShots()
        {
            weapon.PullTrigger();
            weapon.Tick(0f);
            weapon.ReleaseTrigger();
            weapon.Tick(10f);

            weapon.PullTrigger();
            weapon.Tick(0f);

            Assert.That(shotsFired, Is.EqualTo(2));
        }

        [Test]
        public void EmptyingTheMagazineStartsAReload()
        {
            EmptyMagazine();

            Assert.That(weapon.RoundsInMagazine, Is.EqualTo(0));
            Assert.That(weapon.IsReloading, Is.True);
            Assert.That(reloadsStarted, Is.EqualTo(1));
        }

        [Test]
        public void ALongFrameNeverFiresMoreThanTheMagazineHolds()
        {
            weapon.PullTrigger();
            weapon.Tick(Gap * (MagazineSize + 5));

            Assert.That(shotsFired, Is.EqualTo(MagazineSize));
            Assert.That(weapon.IsReloading, Is.True);
        }

        [Test]
        public void NothingFiresWhileReloading()
        {
            EmptyMagazine();

            weapon.Tick(ReloadSeconds - Gap);

            Assert.That(shotsFired, Is.EqualTo(MagazineSize));
        }

        [Test]
        public void AReloadRefillsTheMagazineAfterTheReloadTime()
        {
            EmptyMagazine();
            weapon.ReleaseTrigger();

            weapon.Tick(ReloadSeconds * 0.5f);
            Assert.That(weapon.IsReloading, Is.True);
            Assert.That(weapon.ReloadProgress, Is.EqualTo(0.5f).Within(1e-5f));

            weapon.Tick(ReloadSeconds * 0.5f);
            Assert.That(weapon.IsReloading, Is.False);
            Assert.That(weapon.RoundsInMagazine, Is.EqualTo(MagazineSize));
            Assert.That(reloadsFinished, Is.EqualTo(1));
        }

        [Test]
        public void HoldingTheTriggerThroughAReloadFiresAgainWhenItEnds()
        {
            EmptyMagazine();

            weapon.Tick(ReloadSeconds);

            Assert.That(shotsFired, Is.EqualTo(MagazineSize + 1));
            Assert.That(weapon.RoundsInMagazine, Is.EqualTo(MagazineSize - 1));
        }

        [Test]
        public void ReloadingByHandRefillsAPartlyEmptyMagazine()
        {
            weapon.PullTrigger();
            weapon.Tick(Gap * 2f);
            weapon.ReleaseTrigger();

            Assert.That(weapon.Reload(), Is.True);
            Assert.That(weapon.IsReloading, Is.True);

            weapon.Tick(ReloadSeconds);
            Assert.That(weapon.RoundsInMagazine, Is.EqualTo(MagazineSize));
        }

        [Test]
        public void ReloadingAFullMagazineDoesNothing()
        {
            Assert.That(weapon.Reload(), Is.False);
            Assert.That(weapon.IsReloading, Is.False);
            Assert.That(reloadsStarted, Is.EqualTo(0));
        }

        [Test]
        public void ReloadingWhileReloadingDoesNotRestartTheReload()
        {
            EmptyMagazine();
            weapon.ReleaseTrigger();
            weapon.Tick(ReloadSeconds * 0.5f);

            Assert.That(weapon.Reload(), Is.False);
            weapon.Tick(ReloadSeconds * 0.5f);

            Assert.That(weapon.IsReloading, Is.False);
            Assert.That(reloadsStarted, Is.EqualTo(1));
        }

        [Test]
        public void TimeCannotRunBackwards()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => weapon.Tick(-0.1f));
        }

        [Test]
        public void StatsRejectImpossibleWeapons()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PrimaryWeaponStats(0f, MagazineSize, ReloadSeconds));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PrimaryWeaponStats(ShotsPerSecond, 0, ReloadSeconds));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PrimaryWeaponStats(ShotsPerSecond, MagazineSize, -1f));
        }

        // Holds the trigger until the last shot leaves, which starts the automatic reload.
        private void EmptyMagazine()
        {
            weapon.PullTrigger();
            weapon.Tick(0f);
            for (var i = 1; i < MagazineSize; i++)
            {
                weapon.Tick(Gap);
            }
        }
    }
}
