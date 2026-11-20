using System;
using DarknessRises.Game.Routes;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Routes
{
    public sealed class RouteProgressRejoinTests
    {
        private const float Tolerance = 1e-4f;
        private const float WalkingSpeed = 2f;

        // A straight 20 m Route east, with a Station at 4 m.
        private static RoutePath East() => new(new[] { Vector3.zero, new Vector3(20f, 0f, 0f) });

        // 3 m north of the Station, where a Boss fight left the character.
        private static readonly Vector3 Off = new(4f, 0f, 3f);

        private static RouteProgress AtStation(out Station station, WalkEasing easing = default, params Station[] later)
        {
            station = new Station(0, 4f);
            var stations = new Station[later.Length + 1];
            stations[0] = station;
            later.CopyTo(stations, 1);
            var progress = new RouteProgress(East(), stations, WalkingSpeed, easing);
            progress.Tick(10f);
            return progress;
        }

        [Test]
        public void Rejoining_puts_the_character_where_it_stands_at_once()
        {
            var progress = AtStation(out _);
            var raised = 0;
            progress.Rejoined += () => raised++;

            progress.Rejoin(Off, 8f);

            Assert.That(progress.IsRejoining, Is.True);
            Assert.That(Vector3.Distance(progress.Position, Off), Is.LessThan(Tolerance));
            Assert.That(progress.Distance, Is.EqualTo(4f).Within(Tolerance), "Distance stays where it left the Route");
            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public void Heads_straight_for_the_join_point_at_the_walking_speed()
        {
            var progress = AtStation(out var station);
            progress.Rejoin(Off, 8f);
            station.Clear();

            progress.Tick(1f);

            var join = new Vector3(8f, 0f, 0f);
            var heading = (join - Off).normalized;
            Assert.That(Vector3.Distance(progress.Direction, heading), Is.LessThan(Tolerance));
            Assert.That(Vector3.Distance(progress.Position, Off + heading * WalkingSpeed), Is.LessThan(Tolerance));
        }

        [Test]
        public void Joins_the_Route_and_walks_on_along_it_with_the_rest_of_the_step()
        {
            var progress = AtStation(out var station);
            progress.Rejoin(Off, 8f);
            station.Clear();

            // The way back is 5 m (a 3-4-5 triangle); 6 m of walking leaves 1 m along the Route.
            progress.Tick(3f);

            Assert.That(progress.IsRejoining, Is.False);
            Assert.That(progress.Distance, Is.EqualTo(9f).Within(Tolerance));
            Assert.That(Vector3.Distance(progress.Position, new Vector3(9f, 0f, 0f)), Is.LessThan(Tolerance));
            Assert.That(Vector3.Distance(progress.Direction, Vector3.right), Is.LessThan(Tolerance));
        }

        [Test]
        public void Already_on_the_Station_walks_the_Route_as_usual()
        {
            var progress = AtStation(out var station);
            var onStation = progress.Position;

            progress.Rejoin(onStation, progress.Distance);

            Assert.That(progress.IsRejoining, Is.False);
            Assert.That(Vector3.Distance(progress.Position, onStation), Is.LessThan(Tolerance));
            station.Clear();
            progress.Tick(1f);
            Assert.That(progress.Distance, Is.EqualTo(6f).Within(Tolerance));
        }

        [Test]
        public void Eases_in_to_a_Station_it_rejoins_at_and_stops_there()
        {
            var easing = new WalkEasing(2f, 0f, 0.25f);
            var farDoor = new Station(1, 8f);
            var progress = AtStation(out var station, easing, farDoor);
            progress.Rejoin(Off, 8f);
            station.Clear();

            progress.Tick(1f);
            Assert.That(progress.SpeedFraction, Is.EqualTo(1f).Within(Tolerance), "5 m out, full pace");

            for (var i = 0; i < 600 && !progress.IsAtStation; i++)
            {
                progress.Tick(1f / 60f);
                if (!progress.IsAtStation && i > 60)
                {
                    Assert.That(progress.SpeedFraction, Is.LessThan(1f), "slows over the last metres of the way back");
                }
            }

            Assert.That(progress.CurrentStation, Is.SameAs(farDoor));
            Assert.That(Vector3.Distance(progress.Position, new Vector3(8f, 0f, 0f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void Sets_off_with_the_usual_easing()
        {
            var easing = new WalkEasing(0f, 1f, 0.5f);
            var progress = AtStation(out var station, easing);
            progress.Rejoin(Off, 8f);
            station.Clear();

            progress.Tick(0.1f);

            Assert.That(progress.SpeedFraction, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void The_point_ahead_follows_the_way_back_then_the_Route_without_a_jump()
        {
            var progress = AtStation(out var station);
            progress.Rejoin(Off, 8f);
            station.Clear();

            Assert.That(Vector3.Distance(progress.PositionAhead(5f), new Vector3(8f, 0f, 0f)), Is.LessThan(Tolerance));
            Assert.That(Vector3.Distance(progress.PositionAhead(7f), new Vector3(10f, 0f, 0f)), Is.LessThan(Tolerance));

            var previous = progress.PositionAhead(3f);
            for (var i = 0; i < 300; i++)
            {
                progress.Tick(1f / 60f);
                var ahead = progress.PositionAhead(3f);
                Assert.That(Vector3.Distance(previous, ahead), Is.LessThanOrEqualTo(WalkingSpeed / 60f + Tolerance));
                previous = ahead;
            }
        }

        [Test]
        public void Rejoining_outside_where_it_left_and_its_next_stop_is_refused()
        {
            var farDoor = new Station(1, 8f);
            var progress = AtStation(out _, default, farDoor);

            Assert.Throws<ArgumentOutOfRangeException>(() => progress.Rejoin(Off, 3f));
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.Rejoin(Off, 9f));
        }

        [Test]
        public void Skipping_ahead_drops_the_way_back()
        {
            var farDoor = new Station(1, 8f);
            var progress = AtStation(out _, default, farDoor);
            progress.Rejoin(Off, 6f);

            Assert.That(progress.SkipTo(farDoor), Is.True);

            Assert.That(progress.IsRejoining, Is.False);
            Assert.That(Vector3.Distance(progress.Position, new Vector3(8f, 0f, 0f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void Walking_back_onto_the_Route_does_not_allocate()
        {
            var progress = AtStation(out var station);
            progress.Rejoin(new Vector3(4f, 0f, 400f), 20f);
            station.Clear();

            AllocationAssert.DoesNotAllocate(() =>
            {
                progress.Tick(1f / 60f);
                _ = progress.Position;
                _ = progress.Direction;
                _ = progress.PositionAhead(3f);
            });
        }
    }
}
