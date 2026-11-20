using System;
using DarknessRises.Game.Weapons;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Weapons
{
    public sealed class CarriedPrimariesTests
    {
        private static readonly string[] All = { "Pistol", "Machine gun", "Flamethrower" };

        [Test]
        public void A_Run_carries_only_the_chosen_Primary()
        {
            var carried = CarriedPrimaries.For(All, All[1], carryAll: false, out var equipped);

            Assert.That(carried, Is.EqualTo(new[] { "Machine gun" }));
            Assert.That(equipped, Is.Zero);
        }

        [Test]
        public void A_debug_build_carries_them_all_with_the_chosen_one_equipped()
        {
            var carried = CarriedPrimaries.For(All, All[2], carryAll: true, out var equipped);

            Assert.That(carried, Is.EqualTo(All));
            Assert.That(carried, Is.Not.SameAs(All));
            Assert.That(equipped, Is.EqualTo(2));
        }

        [Test]
        public void The_starting_Primary_is_equipped_first_either_way()
        {
            CarriedPrimaries.For(All, All[0], carryAll: true, out var equippedOfAll);
            CarriedPrimaries.For(All, All[0], carryAll: false, out var equippedAlone);

            Assert.That(equippedOfAll, Is.Zero);
            Assert.That(equippedAlone, Is.Zero);
        }

        [Test]
        public void The_chosen_Primary_must_be_one_there_is()
        {
            Assert.That(() => CarriedPrimaries.For(All, "Railgun", false, out _), Throws.InstanceOf<ArgumentException>());
            Assert.That(() => CarriedPrimaries.For(All, null, false, out _), Throws.InstanceOf<ArgumentNullException>());
            Assert.That(() => CarriedPrimaries.For<string>(null, "Pistol", false, out _), Throws.InstanceOf<ArgumentNullException>());
        }
    }
}
