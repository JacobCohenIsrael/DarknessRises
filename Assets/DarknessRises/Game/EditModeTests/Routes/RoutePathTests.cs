using System;
using DarknessRises.Game.Routes;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Routes
{
    public sealed class RoutePathTests
    {
        private const float Tolerance = 1e-4f;

        // An L: 4 m north, then 3 m east.
        private static RoutePath LShape() => new(new[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 4f),
            new Vector3(3f, 0f, 4f),
        });

        [Test]
        public void Length_is_the_sum_of_its_legs()
        {
            var path = LShape();

            Assert.That(path.Length, Is.EqualTo(7f).Within(Tolerance));
            Assert.That(path.DistanceAtPoint(0), Is.EqualTo(0f).Within(Tolerance));
            Assert.That(path.DistanceAtPoint(1), Is.EqualTo(4f).Within(Tolerance));
            Assert.That(path.DistanceAtPoint(2), Is.EqualTo(7f).Within(Tolerance));
        }

        [Test]
        public void Position_follows_each_leg_in_turn()
        {
            var path = LShape();

            AssertNear(path.PositionAt(0f), new Vector3(0f, 0f, 0f));
            AssertNear(path.PositionAt(2f), new Vector3(0f, 0f, 2f));
            AssertNear(path.PositionAt(4f), new Vector3(0f, 0f, 4f));
            AssertNear(path.PositionAt(5.5f), new Vector3(1.5f, 0f, 4f));
            AssertNear(path.PositionAt(7f), new Vector3(3f, 0f, 4f));
        }

        [Test]
        public void Position_is_clamped_to_the_ends()
        {
            var path = LShape();

            AssertNear(path.PositionAt(-3f), new Vector3(0f, 0f, 0f));
            AssertNear(path.PositionAt(100f), new Vector3(3f, 0f, 4f));
        }

        [Test]
        public void Direction_is_the_leg_ahead_and_the_last_leg_at_the_end()
        {
            var path = LShape();

            AssertNear(path.DirectionAt(1f), Vector3.forward);
            AssertNear(path.DirectionAt(4f), Vector3.right, "at a corner it already faces the next leg");
            AssertNear(path.DirectionAt(7f), Vector3.right);
            AssertNear(path.DirectionAt(100f), Vector3.right);
        }

        [Test]
        public void Repeated_points_are_walked_through_without_a_broken_direction()
        {
            var path = new RoutePath(new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, 2f),
                new Vector3(0f, 0f, 2f),
                new Vector3(2f, 0f, 2f),
                new Vector3(2f, 0f, 2f),
            });

            Assert.That(path.Length, Is.EqualTo(4f).Within(Tolerance));
            AssertNear(path.DirectionAt(2f), Vector3.right);
            AssertNear(path.DirectionAt(4f), Vector3.right);
            AssertNear(path.PositionAt(3f), new Vector3(1f, 0f, 2f));
        }

        [Test]
        public void A_Route_needs_two_points_and_some_length()
        {
            Assert.Throws<ArgumentException>(() => new RoutePath(new[] { Vector3.zero }));
            Assert.Throws<ArgumentException>(() => new RoutePath(new[] { Vector3.one, Vector3.one }));
            Assert.Throws<ArgumentNullException>(() => new RoutePath(null));
        }

        private static void AssertNear(Vector3 actual, Vector3 expected, string message = null)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(Tolerance), $"{message} expected {expected} but was {actual}");
        }
    }
}
