using System;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Weapons;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Runs
{
    /// <summary>
    /// The Primary weapons a Run carries: armed once, kept from Level to Level with their ammo, and gone when the next
    /// Run starts.
    /// </summary>
    public sealed class RunFlowLoadoutTests
    {
        private static readonly PrimaryWeaponStats PistolStats = new(4f, 12, 1f);
        private static readonly PrimaryWeaponStats MachineGunStats = new(8f, 4, 1f);

        private RunFlow run;

        [SetUp]
        public void SetUp()
        {
            run = new RunFlow();
        }

        private static PrimaryWeaponLoadout NewLoadout()
        {
            return new PrimaryWeaponLoadout(new[]
            {
                new PrimaryWeapon(PistolStats),
                new PrimaryWeapon(MachineGunStats, new AmmoReserve(8)),
            });
        }

        // Clears the Level the Run is in and starts the next one.
        private void ClearTheLevel()
        {
            run.FinishStartBeat();
            run.BeginBossFight();
            run.BeginLevelEnd();
            Assert.That(run.FinishLevel(), Is.True);
        }

        [Test]
        public void A_Run_starts_with_no_weapons_armed()
        {
            Assert.That(run.Loadout, Is.Null);

            run.StartNewRun();

            Assert.That(run.Loadout, Is.Null);
        }

        [Test]
        public void Weapons_can_only_be_armed_while_a_Run_is_on()
        {
            Assert.That(run.Carry(NewLoadout()), Is.False, "Before any Run.");
            Assert.That(run.Loadout, Is.Null);

            run.StartNewRun();
            var loadout = NewLoadout();

            Assert.That(run.Carry(loadout), Is.True);
            Assert.That(run.Loadout, Is.SameAs(loadout));

            run.Die();
            run.Exit();

            Assert.That(run.Carry(NewLoadout()), Is.False, "After the Run ended.");
            Assert.That(() => run.Carry(null), Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void The_next_Level_finds_the_weapons_as_the_last_one_left_them()
        {
            run.StartNewRun();
            var loadout = NewLoadout();
            run.Carry(loadout);
            loadout.Switch(1);
            loadout.Equipped.PullTrigger();
            loadout.Tick(0f);
            loadout.Equipped.ReleaseTrigger();

            ClearTheLevel();

            Assert.That(run.LevelIndex, Is.EqualTo(2));
            Assert.That(run.Loadout, Is.SameAs(loadout));
            Assert.That(run.Loadout.EquippedIndex, Is.EqualTo(1));
            Assert.That(run.Loadout.Equipped.RoundsInMagazine, Is.EqualTo(3));
        }

        [Test]
        public void A_Continue_keeps_the_weapons()
        {
            run.StartNewRun();
            var loadout = NewLoadout();
            run.Carry(loadout);
            run.FinishStartBeat();
            run.Die();

            Assert.That(run.Continue(1), Is.True);

            Assert.That(run.Loadout, Is.SameAs(loadout));
        }

        [Test]
        public void A_new_Run_starts_without_the_last_Runs_weapons()
        {
            run.StartNewRun();
            run.Carry(NewLoadout());
            run.Die();
            run.Exit();

            Assert.That(run.StartNewRun(), Is.True);

            Assert.That(run.Loadout, Is.Null, "The new Run arms Weapons of its own, with full reserves.");
        }

        [Test]
        public void A_Restart_starts_without_the_last_Runs_weapons()
        {
            run.StartNewRun();
            run.Carry(NewLoadout());
            run.Die();

            Assert.That(run.Restart(), Is.True);

            Assert.That(run.Loadout, Is.Null);
        }
    }
}
