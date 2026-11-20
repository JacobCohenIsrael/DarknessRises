using DarknessRises.Game.Weapons;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Weapons
{
    /// <summary>A Primary weapon whose ammo runs out: spending it, reloading from the reserve, and running dry.</summary>
    public sealed class PrimaryWeaponAmmoTests
    {
        // 4 shots per second is a gap of exactly 0.25 s, which floats represent exactly.
        private const float ShotsPerSecond = 4f;
        private const float Gap = 0.25f;
        private const int MagazineSize = 4;
        private const float ReloadSeconds = 1f;

        // A magazine and a half beyond the one in the Weapon: ten shots in all.
        private const int ReserveRounds = 6;

        private PrimaryWeapon weapon;
        private int shotsFired;
        private int reloadsStarted;
        private int timesRanDry;

        [SetUp]
        public void SetUp()
        {
            weapon = new PrimaryWeapon(new PrimaryWeaponStats(ShotsPerSecond, MagazineSize, ReloadSeconds), new AmmoReserve(ReserveRounds));
            shotsFired = 0;
            reloadsStarted = 0;
            timesRanDry = 0;
            weapon.Fired += () => shotsFired++;
            weapon.ReloadStarted += () => reloadsStarted++;
            weapon.RanDry += _ => timesRanDry++;
        }

        // Holds the trigger until the magazine is empty, then lets go. Nothing of a reload passes.
        private void EmptyTheMagazine()
        {
            weapon.PullTrigger();
            weapon.Tick(0f);
            weapon.Tick(Gap * MagazineSize);
            weapon.ReleaseTrigger();
        }

        private void WaitOutTheReload()
        {
            weapon.Tick(ReloadSeconds);
        }

        // Fires everything the Weapon has: the magazine, a full reload, and the half magazine the reserve has left.
        private void FireEverything()
        {
            EmptyTheMagazine();
            WaitOutTheReload();
            EmptyTheMagazine();
            WaitOutTheReload();
            EmptyTheMagazine();
        }

        [Test]
        public void It_starts_with_a_full_magazine_and_a_full_reserve()
        {
            Assert.That(weapon.RoundsInMagazine, Is.EqualTo(MagazineSize));
            Assert.That(weapon.Reserve.Rounds, Is.EqualTo(ReserveRounds));
            Assert.That(weapon.HasAmmo, Is.True);
        }

        [Test]
        public void Firing_spends_the_magazine_not_the_reserve()
        {
            weapon.PullTrigger();
            weapon.Tick(0f);
            weapon.Tick(Gap);

            Assert.That(shotsFired, Is.EqualTo(2));
            Assert.That(weapon.RoundsInMagazine, Is.EqualTo(MagazineSize - 2));
            Assert.That(weapon.Reserve.Rounds, Is.EqualTo(ReserveRounds));
        }

        [Test]
        public void A_manual_reload_takes_from_the_reserve_what_the_magazine_is_missing()
        {
            weapon.PullTrigger();
            weapon.Tick(0f);
            weapon.ReleaseTrigger();

            Assert.That(weapon.Reload(), Is.True);
            Assert.That(weapon.Reserve.Rounds, Is.EqualTo(ReserveRounds), "The rounds leave the reserve when the reload ends.");
            WaitOutTheReload();

            Assert.That(weapon.RoundsInMagazine, Is.EqualTo(MagazineSize));
            Assert.That(weapon.Reserve.Rounds, Is.EqualTo(ReserveRounds - 1));
        }

        [Test]
        public void An_empty_magazine_reloads_from_the_reserve_on_its_own()
        {
            EmptyTheMagazine();

            Assert.That(weapon.IsReloading, Is.True);
            Assert.That(timesRanDry, Is.Zero);
            WaitOutTheReload();

            Assert.That(weapon.RoundsInMagazine, Is.EqualTo(MagazineSize));
            Assert.That(weapon.Reserve.Rounds, Is.EqualTo(ReserveRounds - MagazineSize));
        }

        [Test]
        public void A_reload_fills_the_magazine_only_as_far_as_the_reserve_allows()
        {
            EmptyTheMagazine();
            WaitOutTheReload();
            EmptyTheMagazine();
            WaitOutTheReload();

            Assert.That(weapon.RoundsInMagazine, Is.EqualTo(2), "The reserve had 2 rounds left for a magazine of 4.");
            Assert.That(weapon.Reserve.IsEmpty, Is.True);
            Assert.That(weapon.HasAmmo, Is.True, "The rounds in the magazine still fire.");
        }

        [Test]
        public void With_an_empty_reserve_there_is_nothing_to_reload()
        {
            EmptyTheMagazine();
            WaitOutTheReload();
            EmptyTheMagazine();
            WaitOutTheReload();
            var reloads = reloadsStarted;

            Assert.That(weapon.Reload(), Is.False);

            Assert.That(weapon.IsReloading, Is.False);
            Assert.That(reloadsStarted, Is.EqualTo(reloads));
        }

        [Test]
        public void Firing_the_last_round_with_an_empty_reserve_runs_the_Weapon_dry()
        {
            FireEverything();

            Assert.That(shotsFired, Is.EqualTo(MagazineSize + ReserveRounds), "Every round it carried, and not one more.");
            Assert.That(timesRanDry, Is.EqualTo(1));
            Assert.That(weapon.RoundsInMagazine, Is.Zero);
            Assert.That(weapon.HasAmmo, Is.False);
            Assert.That(weapon.IsReloading, Is.False, "There is nothing to reload from.");
            Assert.That(reloadsStarted, Is.EqualTo(2));
        }

        [Test]
        public void A_dry_Weapon_neither_fires_nor_reloads()
        {
            FireEverything();
            var shots = shotsFired;

            weapon.PullTrigger();
            weapon.Tick(5f);

            Assert.That(shotsFired, Is.EqualTo(shots));
            Assert.That(weapon.Reload(), Is.False);
            Assert.That(weapon.IsReloading, Is.False);
            Assert.That(timesRanDry, Is.EqualTo(1), "It ran dry once.");
        }

        [Test]
        public void A_refilled_reserve_puts_a_dry_Weapon_back_to_work()
        {
            FireEverything();
            var shots = shotsFired;

            weapon.Reserve.Refill(0.5f);

            Assert.That(weapon.HasAmmo, Is.True);
            weapon.Tick(0f);
            Assert.That(weapon.IsReloading, Is.True, "An empty magazine reloads as soon as there is ammo for it.");
            WaitOutTheReload();
            Assert.That(weapon.RoundsInMagazine, Is.EqualTo(3), "Half of a reserve of 6.");
            Assert.That(weapon.Reserve.IsEmpty, Is.True);

            weapon.PullTrigger();
            weapon.Tick(0f);
            Assert.That(shotsFired, Is.EqualTo(shots + 1));
        }

        [Test]
        public void A_Weapon_given_no_reserve_never_runs_dry()
        {
            var pistol = new PrimaryWeapon(new PrimaryWeaponStats(ShotsPerSecond, MagazineSize, ReloadSeconds));
            var dry = 0;
            pistol.RanDry += _ => dry++;

            for (var magazine = 0; magazine < 20; magazine++)
            {
                pistol.PullTrigger();
                pistol.Tick(0f);
                pistol.Tick(Gap * MagazineSize);
                pistol.ReleaseTrigger();
                pistol.Tick(ReloadSeconds);
            }

            Assert.That(pistol.Reserve.IsUnlimited, Is.True);
            Assert.That(pistol.RoundsInMagazine, Is.EqualTo(MagazineSize));
            Assert.That(pistol.HasAmmo, Is.True);
            Assert.That(dry, Is.Zero);
        }

        [Test]
        public void Dropped_listeners_hear_nothing_more()
        {
            var reserveChanges = 0;
            weapon.Reserve.Changed += () => reserveChanges++;
            weapon.DropListeners();

            FireEverything();

            Assert.That(shotsFired, Is.Zero);
            Assert.That(reloadsStarted, Is.Zero);
            Assert.That(timesRanDry, Is.Zero);
            Assert.That(reserveChanges, Is.Zero);
            Assert.That(weapon.HasAmmo, Is.False, "The Weapon itself carried on.");
        }
    }
}
