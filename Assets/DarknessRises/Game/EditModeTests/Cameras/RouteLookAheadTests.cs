using DarknessRises.Game.Cameras;
using DarknessRises.Game.Routes;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Cameras
{
    public sealed class RouteLookAheadTests
    {
        private const float Tolerance = 1e-4f;
        private const float WalkingLookAhead = 3f;
        private const float StoppedLookAhead = 1f;
        private const float ChangeSpeed = 4f;

        // An L: 4 m north, then 6 m east, with a Station on the corner.
        private static RouteProgress LShapeWithStationOnCorner(out Station station)
        {
            var path = new RoutePath(new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, 4f),
                new Vector3(6f, 0f, 4f),
            });
            station = new Station(0, 4f);
            return new RouteProgress(path, new[] { station }, 1f);
        }

        private static RouteLookAhead NewLookAhead() => new(WalkingLookAhead, StoppedLookAhead, ChangeSpeed);

        [Test]
        public void Camera_aims_ahead_of_the_walking_character_along_the_Route()
        {
            var progress = LShapeWithStationOnCorner(out _);
            var lookAhead = NewLookAhead();

            progress.Tick(2f);
            lookAhead.Tick(0f, progress);

            // 2 m walked + 3 m ahead = 5 m along the Route: round the corner, 1 m east.
            AssertNear(lookAhead.Target, new Vector3(1f, 0f, 4f));
        }

        [Test]
        public void Look_ahead_eases_back_while_the_character_is_at_a_Station()
        {
            var progress = LShapeWithStationOnCorner(out _);
            var lookAhead = NewLookAhead();
            progress.Tick(4f);

            lookAhead.Tick(0.25f, progress);
            Assert.That(lookAhead.Distance, Is.EqualTo(2f).Within(Tolerance), "changes at the authored speed");

            lookAhead.Tick(10f, progress);
            Assert.That(lookAhead.Distance, Is.EqualTo(StoppedLookAhead).Within(Tolerance));
            AssertNear(lookAhead.Target, new Vector3(1f, 0f, 4f));
        }

        [Test]
        public void Look_ahead_grows_again_when_the_character_walks_on()
        {
            var progress = LShapeWithStationOnCorner(out var station);
            var lookAhead = NewLookAhead();
            progress.Tick(4f);
            lookAhead.Tick(10f, progress);

            station.Clear();
            progress.Tick(0.25f);
            lookAhead.Tick(0.25f, progress);

            Assert.That(lookAhead.Distance, Is.EqualTo(StoppedLookAhead + ChangeSpeed * 0.25f).Within(Tolerance));
        }

        [Test]
        public void Look_ahead_never_aims_past_the_end_of_the_Route()
        {
            var progress = LShapeWithStationOnCorner(out var station);
            var lookAhead = NewLookAhead();
            progress.Tick(4f);
            station.Clear();
            progress.Tick(5f);

            lookAhead.Tick(0f, progress);

            AssertNear(lookAhead.Target, new Vector3(6f, 0f, 4f));
        }

        private static void AssertNear(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(Tolerance), $"expected {expected} but was {actual}");
        }
    }
}
