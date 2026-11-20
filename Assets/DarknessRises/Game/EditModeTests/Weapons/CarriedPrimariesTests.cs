using System;
using System.Collections.Generic;
using DarknessRises.Game.Weapons;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Weapons
{
    /// <summary>Which Primary weapons come along on a Run: the unlocked ones, and no other.</summary>
    public sealed class CarriedPrimariesTests
    {
        private const string Pistol = "Pistol";
        private const string MachineGun = "Machine gun";
        private const string Flamethrower = "Flamethrower";

        // Every Primary there is, in the Shop's order.
        private static readonly string[] All = { Pistol, MachineGun, Flamethrower };

        private HashSet<string> unlocked;

        [SetUp]
        public void SetUp()
        {
            unlocked = new HashSet<string> { Pistol };
        }

        private string[] Carried(out int[] places) => CarriedPrimaries.For(All, weapon => unlocked.Contains(weapon), out places);

        [Test]
        public void A_new_player_carries_only_the_Pistol()
        {
            var carried = Carried(out var places);

            Assert.That(carried, Is.EqualTo(new[] { Pistol }));
            Assert.That(places, Is.EqualTo(new[] { 0, -1, -1 }));
        }

        [Test]
        public void Every_unlocked_Primary_comes_along_in_the_Shops_order()
        {
            unlocked.Add(Flamethrower);
            unlocked.Add(MachineGun);

            var carried = Carried(out var places);

            Assert.That(carried, Is.EqualTo(All));
            Assert.That(carried, Is.Not.SameAs(All));
            Assert.That(places, Is.EqualTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void A_Primary_that_isnt_unlocked_isnt_carried_and_has_no_place()
        {
            unlocked.Add(Flamethrower);

            var carried = Carried(out var places);

            Assert.That(carried, Is.EqualTo(new[] { Pistol, Flamethrower }));
            Assert.That(places[1], Is.EqualTo(-1), "The Machine gun's key finds nothing to equip.");
            Assert.That(places[2], Is.EqualTo(1), "The Flamethrower's key finds it second among the carried ones.");
        }

        [Test]
        public void The_Primary_owned_from_the_start_comes_first_so_a_Run_starts_with_it_equipped()
        {
            unlocked.Add(MachineGun);
            unlocked.Add(Flamethrower);

            var carried = Carried(out _);
            var loadout = new PrimaryWeaponLoadout(new[]
            {
                new PrimaryWeapon(new PrimaryWeaponStats(4f, 12, 1.2f)),
                new PrimaryWeapon(new PrimaryWeaponStats(10f, 30, 2.5f), new AmmoReserve(240)),
                new PrimaryWeapon(new PrimaryWeaponStats(15f, 60, 2.5f), new AmmoReserve(300)),
            });

            Assert.That(carried[0], Is.EqualTo(Pistol));
            Assert.That(loadout.EquippedIndex, Is.Zero);
        }

        [Test]
        public void One_Primary_must_be_owned()
        {
            unlocked.Clear();

            Assert.That(() => Carried(out _), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void It_needs_the_Primaries_and_a_way_to_tell_which_are_unlocked()
        {
            Assert.That(() => CarriedPrimaries.For<string>(null, _ => true, out _), Throws.InstanceOf<ArgumentNullException>());
            Assert.That(() => CarriedPrimaries.For(All, null, out _), Throws.InstanceOf<ArgumentNullException>());
        }
    }
}
