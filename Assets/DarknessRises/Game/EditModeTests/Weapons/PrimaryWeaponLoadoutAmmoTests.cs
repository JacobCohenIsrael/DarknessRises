using System;
using System.Collections.Generic;
using DarknessRises.Game.Weapons;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Weapons
{
    /// <summary>
    /// Switching between the carried Primaries when ammo runs out: which can be selected, the switch back to the Pistol
    /// when the equipped one runs dry, and refills.
    /// </summary>
    public sealed class PrimaryWeaponLoadoutAmmoTests
    {
        private const int Pistol = 0;
        private const int MachineGun = 1;
        private const int Flamethrower = 2;

        // Fire rates that are powers of two apart, so the gaps between shots are exact in floats.
        private static readonly PrimaryWeaponStats PistolStats = new(4f, 12, 1f);
        private static readonly PrimaryWeaponStats MachineGunStats = new(8f, 4, 1f);
        private static readonly PrimaryWeaponStats FlamethrowerStats = new(16f, 8, 1f);

        // The Machine gun carries one magazine beyond the one loaded; the Flamethrower only its tank.
        private const int MachineGunReserve = 4;
        private const int FlamethrowerReserve = 0;

        private PrimaryWeaponLoadout loadout;
        private List<string> heard;

        [SetUp]
        public void SetUp()
        {
            loadout = new PrimaryWeaponLoadout(new[]
            {
                new PrimaryWeapon(PistolStats),
                new PrimaryWeapon(MachineGunStats, new AmmoReserve(MachineGunReserve)),
                new PrimaryWeapon(FlamethrowerStats, new AmmoReserve(FlamethrowerReserve)),
            });
            heard = new List<string>();
            loadout.RanDry += index => heard.Add($"dry {index}");
            loadout.Switched += weapon => heard.Add($"switched {Array.IndexOf(new[] { loadout[Pistol], loadout[MachineGun], loadout[Flamethrower] }, weapon)}");
        }

        // Equips the Primary and holds its trigger until it has nothing left, through its reloads.
        private void RunDry(int index)
        {
            Assert.That(loadout.Switch(index), Is.True);
            for (var second = 0; second < 100 && loadout[index].HasAmmo; second++)
            {
                loadout[index].PullTrigger();
                loadout.Tick(1f);
            }

            Assert.That(loadout[index].HasAmmo, Is.False);
            heard.Clear();
        }

        [Test]
        public void Every_carried_Primary_can_be_selected_while_it_has_ammo()
        {
            Assert.That(loadout.CanSelect(Pistol), Is.True);
            Assert.That(loadout.CanSelect(MachineGun), Is.True);
            Assert.That(loadout.CanSelect(Flamethrower), Is.True, "Its reserve is empty, but its tank is full.");
            Assert.That(loadout.CanSelect(3), Is.False);
            Assert.That(loadout.CanSelect(-1), Is.False);
        }

        [Test]
        public void When_the_equipped_Primary_runs_dry_the_Pistol_takes_over_on_its_own()
        {
            loadout.Switch(MachineGun);
            heard.Clear();

            for (var second = 0; second < 10; second++)
            {
                loadout.Equipped.PullTrigger();
                loadout.Tick(1f);
                if (loadout.EquippedIndex != MachineGun)
                {
                    break;
                }
            }

            Assert.That(loadout.EquippedIndex, Is.EqualTo(Pistol));
            Assert.That(loadout[MachineGun].RoundsInMagazine, Is.Zero);
            Assert.That(loadout[MachineGun].Reserve.IsEmpty, Is.True);
            Assert.That(heard, Is.EqualTo(new[] { $"dry {MachineGun}", $"switched {Pistol}" }), "It tells that the Weapon ran dry, then that the Pistol is equipped.");
        }

        [Test]
        public void The_trigger_of_the_Primary_that_ran_dry_is_let_go_and_the_Pistol_fires_on_the_next_pull()
        {
            RunDry(MachineGun);

            Assert.That(loadout[MachineGun].IsTriggerHeld, Is.False);
            Assert.That(loadout[Pistol].RoundsInMagazine, Is.EqualTo(12), "Switching fired nothing.");

            loadout.Equipped.PullTrigger();
            loadout.Tick(0f);

            Assert.That(loadout[Pistol].RoundsInMagazine, Is.EqualTo(11));
        }

        [Test]
        public void A_Primary_that_ran_dry_cant_be_selected()
        {
            RunDry(MachineGun);

            Assert.That(loadout.CanSelect(MachineGun), Is.False);
            Assert.That(loadout.Switch(MachineGun), Is.False);

            Assert.That(loadout.EquippedIndex, Is.EqualTo(Pistol));
            Assert.That(heard, Is.Empty);
        }

        [Test]
        public void Next_and_previous_walk_the_carried_Primaries_and_wrap_round()
        {
            Assert.That(loadout.SwitchToNext(), Is.True);
            Assert.That(loadout.EquippedIndex, Is.EqualTo(MachineGun));
            loadout.SwitchToNext();
            Assert.That(loadout.EquippedIndex, Is.EqualTo(Flamethrower));
            loadout.SwitchToNext();
            Assert.That(loadout.EquippedIndex, Is.EqualTo(Pistol));

            Assert.That(loadout.SwitchToPrevious(), Is.True);
            Assert.That(loadout.EquippedIndex, Is.EqualTo(Flamethrower));
            loadout.SwitchToPrevious();
            Assert.That(loadout.EquippedIndex, Is.EqualTo(MachineGun));
            loadout.SwitchToPrevious();
            Assert.That(loadout.EquippedIndex, Is.EqualTo(Pistol));
        }

        [Test]
        public void Next_and_previous_skip_a_Primary_that_ran_dry()
        {
            RunDry(MachineGun);

            Assert.That(loadout.SwitchToNext(), Is.True);
            Assert.That(loadout.EquippedIndex, Is.EqualTo(Flamethrower), "Past the dry Machine gun.");

            Assert.That(loadout.SwitchToPrevious(), Is.True);
            Assert.That(loadout.EquippedIndex, Is.EqualTo(Pistol), "Back past it.");
        }

        [Test]
        public void With_every_other_Primary_dry_there_is_nothing_to_switch_to()
        {
            RunDry(MachineGun);
            RunDry(Flamethrower);

            Assert.That(loadout.EquippedIndex, Is.EqualTo(Pistol));
            Assert.That(loadout.SwitchToNext(), Is.False);
            Assert.That(loadout.SwitchToPrevious(), Is.False);
            Assert.That(loadout.EquippedIndex, Is.EqualTo(Pistol));
            Assert.That(heard, Is.Empty);
        }

        [Test]
        public void With_only_the_Pistol_there_is_nothing_to_switch_to()
        {
            var alone = new PrimaryWeaponLoadout(new[] { new PrimaryWeapon(PistolStats) });

            Assert.That(alone.SwitchToNext(), Is.False);
            Assert.That(alone.SwitchToPrevious(), Is.False);
            Assert.That(alone.EquippedIndex, Is.Zero);
        }

        [Test]
        public void A_refill_makes_a_dry_Primary_selectable_again()
        {
            RunDry(MachineGun);

            loadout.Refill(0.5f);

            Assert.That(loadout[MachineGun].Reserve.Rounds, Is.EqualTo(2), "Half of a reserve of 4.");
            Assert.That(loadout.CanSelect(MachineGun), Is.True);
            Assert.That(loadout.Switch(MachineGun), Is.True);

            // Its empty magazine reloads from what the refill brought.
            loadout.Tick(0f);
            Assert.That(loadout.Equipped.IsReloading, Is.True);
            loadout.Tick(1f);
            Assert.That(loadout.Equipped.RoundsInMagazine, Is.EqualTo(2));
        }

        [Test]
        public void A_refill_tops_up_every_reserve_and_never_above_full()
        {
            RunDry(MachineGun);

            loadout.Refill(0.5f);
            loadout.Refill(1f);

            Assert.That(loadout[MachineGun].Reserve.Rounds, Is.EqualTo(MachineGunReserve));
            Assert.That(loadout[Flamethrower].Reserve.Rounds, Is.EqualTo(FlamethrowerReserve));
            Assert.That(loadout[Pistol].Reserve.IsUnlimited, Is.True);
            Assert.That(loadout.EquippedIndex, Is.EqualTo(Pistol), "A refill doesn't switch anything.");
        }

        [Test]
        public void A_refill_leaves_the_magazines_as_they_were()
        {
            loadout.Switch(MachineGun);
            loadout.Equipped.PullTrigger();
            loadout.Tick(0f);
            loadout.Equipped.ReleaseTrigger();

            loadout.Refill(1f);

            Assert.That(loadout[MachineGun].RoundsInMagazine, Is.EqualTo(3));
            Assert.That(loadout[MachineGun].Reserve.Rounds, Is.EqualTo(MachineGunReserve));
        }

        [Test]
        public void Without_a_Primary_that_never_runs_out_the_first_one_with_ammo_takes_over()
        {
            var finite = new PrimaryWeaponLoadout(new[]
            {
                new PrimaryWeapon(MachineGunStats, new AmmoReserve(0)),
                new PrimaryWeapon(FlamethrowerStats, new AmmoReserve(0)),
            });

            finite.Equipped.PullTrigger();
            finite.Tick(1f);

            Assert.That(finite.EquippedIndex, Is.EqualTo(1));

            finite.Equipped.PullTrigger();
            finite.Tick(1f);

            Assert.That(finite.EquippedIndex, Is.EqualTo(1), "Nothing is left to switch to.");
            Assert.That(finite.CanSelect(0), Is.False);
            Assert.That(finite.CanSelect(1), Is.False);
        }

        [Test]
        public void After_its_listeners_are_dropped_the_loadout_still_switches_back_to_the_Pistol()
        {
            var shots = 0;
            var reserveChanges = 0;
            loadout[MachineGun].Fired += () => shots++;
            loadout[MachineGun].Reserve.Changed += () => reserveChanges++;

            loadout.DropListeners();
            RunDry(MachineGun);

            Assert.That(loadout.EquippedIndex, Is.EqualTo(Pistol));
            Assert.That(shots, Is.Zero);
            Assert.That(reserveChanges, Is.Zero);
        }

        [Test]
        public void Dropped_listeners_hear_nothing_of_the_next_Level()
        {
            loadout.DropListeners();

            loadout.Switch(MachineGun);
            for (var second = 0; second < 10 && loadout[MachineGun].HasAmmo; second++)
            {
                loadout[MachineGun].PullTrigger();
                loadout.Tick(1f);
            }

            Assert.That(heard, Is.Empty);
        }

        [Test]
        public void A_loadout_needs_the_Primaries_it_carries()
        {
            Assert.That(() => new PrimaryWeaponLoadout((IReadOnlyList<PrimaryWeapon>)null), Throws.InstanceOf<ArgumentException>());
            Assert.That(() => new PrimaryWeaponLoadout(Array.Empty<PrimaryWeapon>()), Throws.InstanceOf<ArgumentException>());
            Assert.That(() => new PrimaryWeaponLoadout(new PrimaryWeapon[] { null }), Throws.InstanceOf<ArgumentNullException>());
        }
    }
}
