using System;
using DarknessRises.Game.Weapons;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Weapons
{
    public sealed class PrimaryWeaponLoadoutTests
    {
        private const int Pistol = 0;
        private const int MachineGun = 1;
        private const int Flamethrower = 2;

        // Fire rates that are powers of two apart, so the gaps between shots are exact in floats.
        private static readonly PrimaryWeaponStats PistolStats = new(4f, 12, 1.5f);
        private static readonly PrimaryWeaponStats MachineGunStats = new(8f, 30, 2.5f);
        private static readonly PrimaryWeaponStats FlamethrowerStats = new(16f, 60, 2f);

        private PrimaryWeaponLoadout loadout;

        [SetUp]
        public void SetUp()
        {
            loadout = new PrimaryWeaponLoadout(new[] { PistolStats, MachineGunStats, FlamethrowerStats });
        }

        [Test]
        public void StartsWithTheFirstPrimaryEquipped()
        {
            Assert.That(loadout.Count, Is.EqualTo(3));
            Assert.That(loadout.EquippedIndex, Is.EqualTo(Pistol));
            Assert.That(loadout.Equipped, Is.SameAs(loadout[Pistol]));
        }

        [Test]
        public void EachPrimaryHasItsOwnMagazineSize()
        {
            Assert.That(loadout[Pistol].MagazineSize, Is.EqualTo(12));
            Assert.That(loadout[MachineGun].MagazineSize, Is.EqualTo(30));
            Assert.That(loadout[Flamethrower].MagazineSize, Is.EqualTo(60));
        }

        [Test]
        public void SwitchingEquipsTheChosenPrimaryAndAnnouncesIt()
        {
            PrimaryWeapon announced = null;
            loadout.Switched += weapon => announced = weapon;

            var switched = loadout.Switch(MachineGun);

            Assert.That(switched, Is.True);
            Assert.That(loadout.EquippedIndex, Is.EqualTo(MachineGun));
            Assert.That(loadout.Equipped, Is.SameAs(loadout[MachineGun]));
            Assert.That(announced, Is.SameAs(loadout[MachineGun]));
        }

        [Test]
        public void SwitchingToTheEquippedPrimaryDoesNothing()
        {
            var announcements = 0;
            loadout.Switched += _ => announcements++;

            Assert.That(loadout.Switch(Pistol), Is.False);
            Assert.That(announcements, Is.EqualTo(0));
        }

        [Test]
        public void SwitchingToAMissingPrimaryThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => loadout.Switch(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => loadout.Switch(-1));
        }

        [Test]
        public void AnEmptyLoadoutIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new PrimaryWeaponLoadout(Array.Empty<PrimaryWeaponStats>()));
        }

        [Test]
        public void OnlyTheEquippedPrimaryFires()
        {
            loadout.Equipped.PullTrigger();
            loadout.Tick(0f);

            Assert.That(loadout[Pistol].RoundsInMagazine, Is.EqualTo(11));
            Assert.That(loadout[MachineGun].RoundsInMagazine, Is.EqualTo(30));
            Assert.That(loadout[Flamethrower].RoundsInMagazine, Is.EqualTo(60));
        }

        [Test]
        public void EachPrimaryKeepsItsOwnMagazineAcrossSwitches()
        {
            // Pistol: 3 shots (at 0, 0.25 and 0.5 s).
            loadout.Equipped.PullTrigger();
            loadout.Tick(0f);
            loadout.Tick(0.5f);

            // Machine gun: 5 shots (at 0, 0.125, 0.25, 0.375 and 0.5 s).
            loadout.Switch(MachineGun);
            loadout.Equipped.PullTrigger();
            loadout.Tick(0f);
            loadout.Tick(0.5f);

            loadout.Switch(Pistol);

            Assert.That(loadout[Pistol].RoundsInMagazine, Is.EqualTo(12 - 3));
            Assert.That(loadout[MachineGun].RoundsInMagazine, Is.EqualTo(30 - 5));
            Assert.That(loadout[Flamethrower].RoundsInMagazine, Is.EqualTo(60));
            Assert.That(loadout.Equipped.RoundsInMagazine, Is.EqualTo(12 - 3));
        }

        [Test]
        public void SwitchingReleasesTheHolsteredTrigger()
        {
            loadout.Equipped.PullTrigger();
            loadout.Tick(0f);

            loadout.Switch(MachineGun);
            loadout.Switch(Pistol);
            loadout.Tick(1f);

            Assert.That(loadout[Pistol].IsTriggerHeld, Is.False);
            Assert.That(loadout[Pistol].RoundsInMagazine, Is.EqualTo(11));
        }

        [Test]
        public void AHolsteredReloadWaitsUntilThePrimaryIsEquippedAgain()
        {
            // Empty the Pistol, which starts its 1.5 s reload, then let 0.5 s of it pass.
            loadout.Equipped.PullTrigger();
            loadout.Tick(0f);
            loadout.Tick(11 * 0.25f);
            loadout.Equipped.ReleaseTrigger();
            Assert.That(loadout[Pistol].IsReloading, Is.True);
            loadout.Tick(0.5f);

            // Time spent holstered doesn't count toward the reload.
            loadout.Switch(MachineGun);
            loadout.Tick(5f);
            loadout.Switch(Pistol);
            Assert.That(loadout[Pistol].IsReloading, Is.True);
            Assert.That(loadout[Pistol].RoundsInMagazine, Is.EqualTo(0));

            loadout.Tick(1f);

            Assert.That(loadout[Pistol].IsReloading, Is.False);
            Assert.That(loadout[Pistol].RoundsInMagazine, Is.EqualTo(12));
        }
    }
}
