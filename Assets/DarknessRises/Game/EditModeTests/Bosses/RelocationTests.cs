using System.Collections.Generic;
using DarknessRises.Game.Bosses;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Bosses
{
    public sealed class RelocationTests
    {
        // A diamond of Positions around a centre one, as seen from the camera (x east, z north).
        private const int South = 0;
        private const int Centre = 1;
        private const int West = 2;
        private const int East = 3;
        private const int North = 4;

        private static readonly Vector3[] Diamond =
        {
            new(0f, 0f, -4f),
            new(0f, 0f, 0f),
            new(-7f, 0f, 0f),
            new(7f, 0f, 0f),
            new(0f, 0f, 5f),
        };

        private static readonly RelocateSettings Settings = new(dashSeconds: 0.25f, maxAngleDegrees: 60f);

        private static Relocation Enabled(int start)
        {
            return new Relocation(Diamond, start, Settings) { IsEnabled = true };
        }

        [TestCase(South, 0f, 1f, Centre)]
        [TestCase(Centre, 0f, 1f, North)]
        [TestCase(Centre, -1f, 0f, West)]
        [TestCase(Centre, 1f, 0f, East)]
        [TestCase(Centre, 0f, -1f, South)]
        [TestCase(West, 1f, 0f, Centre)]
        public void A_direction_picks_the_nearest_Position_that_way(int from, float x, float y, int expected)
        {
            Assert.That(Relocation.PickInDirection(Diamond, from, new Vector2(x, y), 60f), Is.EqualTo(expected));
        }

        [Test]
        public void A_Position_off_to_the_side_counts_when_it_is_within_the_angle()
        {
            // From the west, up-right: the centre lies at 90 degrees, the north Position at about 54.
            Assert.That(Relocation.PickInDirection(Diamond, West, Vector2.up, 60f), Is.EqualTo(North));
        }

        [Test]
        public void Nothing_is_picked_when_no_Position_lies_that_way()
        {
            Assert.That(Relocation.PickInDirection(Diamond, North, Vector2.up, 60f), Is.EqualTo(-1));
            Assert.That(Relocation.PickInDirection(Diamond, West, Vector2.left, 60f), Is.EqualTo(-1));
        }

        [Test]
        public void A_nearer_Position_wins_over_one_straighter_ahead()
        {
            var positions = new List<Vector3> { Vector3.zero, new(0f, 0f, 10f), new(2f, 0f, 3f) };

            Assert.That(Relocation.PickInDirection(positions, 0, Vector2.up, 60f), Is.EqualTo(2));
        }

        [Test]
        public void Relocating_dashes_there_and_arrives_after_the_dash()
        {
            var relocation = Enabled(Centre);
            var arrived = -1;
            relocation.Arrived += position => arrived = position;

            Assert.That(relocation.TryRelocate(Vector2.left), Is.True);
            Assert.That(relocation.IsDashing, Is.True);
            Assert.That(relocation.Destination, Is.EqualTo(West));
            Assert.That(relocation.Position, Is.EqualTo(Centre), "Mid-dash, it hasn't left the Position yet.");

            relocation.Tick(0.1f);
            Assert.That(relocation.Point.x, Is.LessThan(0f).And.GreaterThan(-7f));

            relocation.Tick(0.2f);
            Assert.That(relocation.Position, Is.EqualTo(West));
            Assert.That(relocation.IsDashing, Is.False);
            Assert.That(relocation.Point, Is.EqualTo(Diamond[West]));
            Assert.That(arrived, Is.EqualTo(West));
        }

        [Test]
        public void Relocate_is_only_available_while_the_Boss_fight_enables_it()
        {
            var relocation = new Relocation(Diamond, Centre, Settings);

            Assert.That(relocation.TryRelocate(Vector2.left), Is.False);
            Assert.That(relocation.IsDashing, Is.False);

            relocation.IsEnabled = true;
            Assert.That(relocation.TryRelocate(Vector2.left), Is.True);
        }

        [Test]
        public void A_second_direction_mid_dash_is_ignored()
        {
            var relocation = Enabled(Centre);
            relocation.TryRelocate(Vector2.left);

            Assert.That(relocation.TryRelocate(Vector2.right), Is.False);
            Assert.That(relocation.Destination, Is.EqualTo(West));
        }

        [Test]
        public void A_direction_with_no_Position_that_way_does_nothing()
        {
            var relocation = Enabled(North);

            Assert.That(relocation.TryRelocate(Vector2.up), Is.False);
            Assert.That(relocation.IsDashing, Is.False);
        }

        [Test]
        public void The_fight_can_move_the_character_even_while_Relocate_is_off()
        {
            var relocation = new Relocation(Diamond, East, Settings);

            relocation.MoveTo(South);
            relocation.Tick(1f);

            Assert.That(relocation.Position, Is.EqualTo(South));
        }
    }
}
