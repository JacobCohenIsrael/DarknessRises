using DarknessRises.Game.Weapons;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Weapons
{
    /// <summary>What can fire during the switch beat: the loadout holds the equipped Primary's fire while told to.</summary>
    public sealed class SwitchFireHoldTests
    {
        private const int Pistol = 0;
        private const int MachineGun = 1;

        private static readonly PrimaryWeaponStats PistolStats = new(4f, 12, 1f);
        private static readonly PrimaryWeaponStats MachineGunStats = new(8f, 30, 2f);

        private PrimaryWeaponLoadout loadout;

        [SetUp]
        public void SetUp()
        {
            loadout = new PrimaryWeaponLoadout(new[] { PistolStats, MachineGunStats });
        }

        [Test]
        public void AHeldTriggerDoesntFireWhileFireIsHeld()
        {
            var shots = 0;
            loadout[Pistol].Fired += () => shots++;
            loadout.Equipped.PullTrigger();

            loadout.Tick(0.5f, mayFire: false);

            Assert.That(shots, Is.Zero);
            Assert.That(loadout.Equipped.RoundsInMagazine, Is.EqualTo(12));
        }

        [Test]
        public void HeldFireIsntBankedAsExtraShots()
        {
            var shots = 0;
            loadout[Pistol].Fired += () => shots++;
            loadout.Equipped.PullTrigger();

            loadout.Tick(1f, mayFire: false);
            loadout.Tick(0f, mayFire: true);

            Assert.That(shots, Is.EqualTo(1));
        }

        [Test]
        public void TheNewWeaponFiresTheMomentFireIsAllowed()
        {
            var shots = 0;
            loadout[MachineGun].Fired += () => shots++;
            loadout.Switch(MachineGun);
            loadout.Equipped.PullTrigger();

            loadout.Tick(0.125f, mayFire: false);
            Assert.That(shots, Is.Zero);

            loadout.Tick(0.0625f, mayFire: true);
            Assert.That(shots, Is.EqualTo(1));
        }

        [Test]
        public void AReloadCarriesOnWhileFireIsHeld()
        {
            // Four shots a second: the twelfth, the last in the magazine, goes at 2.75 s and starts the reload.
            var empty = new PrimaryWeapon(PistolStats);
            empty.PullTrigger();
            empty.Tick(2.75f);
            Assert.That(empty.IsReloading, Is.True);

            empty.Tick(1f, mayFire: false);

            Assert.That(empty.IsReloading, Is.False);
            Assert.That(empty.RoundsInMagazine, Is.EqualTo(12));
        }
    }
}
