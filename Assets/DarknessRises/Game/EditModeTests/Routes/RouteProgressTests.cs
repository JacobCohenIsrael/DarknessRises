using System;
using System.Collections.Generic;
using DarknessRises.Game.Routes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DarknessRises.Game.EditModeTests.Routes
{
    public sealed class RouteProgressTests
    {
        private const float Tolerance = 1e-4f;
        private const float WalkingSpeed = 2f;

        // A straight 10 m Route north.
        private static RoutePath Straight() => new(new[] { Vector3.zero, new Vector3(0f, 0f, 10f) });

        [Test]
        public void Character_walks_at_the_walking_speed()
        {
            var progress = new RouteProgress(Straight(), Array.Empty<Station>(), WalkingSpeed);

            progress.Tick(1.5f);

            Assert.That(progress.Distance, Is.EqualTo(3f).Within(Tolerance));
            Assert.That(Vector3.Distance(progress.Position, new Vector3(0f, 0f, 3f)), Is.LessThan(Tolerance));
            Assert.That(Vector3.Distance(progress.Direction, Vector3.forward), Is.LessThan(Tolerance));
            Assert.That(progress.IsAtStation, Is.False);
        }

        [Test]
        public void Character_stops_exactly_at_a_Station_and_waits()
        {
            var station = new Station(0, 3f);
            var progress = new RouteProgress(Straight(), new[] { station }, WalkingSpeed);
            Station reached = null;
            progress.StationReached += s => reached = s;

            progress.Tick(2f);

            Assert.That(progress.Distance, Is.EqualTo(3f).Within(Tolerance), "doesn't overshoot");
            Assert.That(progress.IsAtStation, Is.True);
            Assert.That(progress.CurrentStation, Is.SameAs(station));
            Assert.That(reached, Is.SameAs(station));

            progress.Tick(10f);

            Assert.That(progress.Distance, Is.EqualTo(3f).Within(Tolerance), "waits until cleared");
        }

        [Test]
        public void Clearing_the_Station_lets_the_character_walk_on()
        {
            var station = new Station(0, 3f);
            var progress = new RouteProgress(Straight(), new[] { station }, WalkingSpeed);
            Station left = null;
            progress.StationLeft += s => left = s;
            progress.Tick(2f);

            station.Clear();

            Assert.That(progress.IsAtStation, Is.False);
            Assert.That(left, Is.SameAs(station));

            progress.Tick(1f);

            Assert.That(progress.Distance, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void Character_stops_at_each_Station_in_turn()
        {
            var first = new Station(0, 2f);
            var second = new Station(1, 6f);
            var progress = new RouteProgress(Straight(), new[] { first, second }, WalkingSpeed);
            var reached = new List<Station>();
            progress.StationReached += reached.Add;

            progress.Tick(10f);
            first.Clear();
            progress.Tick(10f);

            Assert.That(progress.Distance, Is.EqualTo(6f).Within(Tolerance));
            Assert.That(reached, Is.EqualTo(new[] { first, second }));
        }

        [Test]
        public void A_Station_cleared_as_soon_as_it_is_reached_still_stops_the_character_for_that_step()
        {
            var station = new Station(0, 3f);
            var progress = new RouteProgress(Straight(), new[] { station }, WalkingSpeed);
            progress.StationReached += s => s.Clear();

            progress.Tick(2f);

            Assert.That(progress.Distance, Is.EqualTo(3f).Within(Tolerance));
            Assert.That(progress.IsAtStation, Is.False);

            progress.Tick(1f);

            Assert.That(progress.Distance, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void A_Station_cleared_before_the_character_arrives_is_walked_through()
        {
            var station = new Station(0, 3f);
            station.Clear();
            var progress = new RouteProgress(Straight(), new[] { station }, WalkingSpeed);
            var reached = 0;
            progress.StationReached += _ => reached++;

            progress.Tick(2f);
            progress.Tick(1f);

            Assert.That(reached, Is.EqualTo(1));
            Assert.That(progress.IsAtStation, Is.False);
            Assert.That(progress.Distance, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void Clearing_a_Station_the_character_is_not_at_does_not_move_it()
        {
            var first = new Station(0, 2f);
            var second = new Station(1, 6f);
            var progress = new RouteProgress(Straight(), new[] { first, second }, WalkingSpeed);
            progress.Tick(5f);

            second.Clear();

            Assert.That(progress.CurrentStation, Is.SameAs(first));
        }

        [Test]
        public void Character_finishes_at_the_end_of_the_Route()
        {
            var progress = new RouteProgress(Straight(), Array.Empty<Station>(), WalkingSpeed);
            var finished = 0;
            progress.Finished += () => finished++;

            progress.Tick(4f);
            Assert.That(progress.IsFinished, Is.False);

            progress.Tick(4f);
            progress.Tick(4f);

            Assert.That(progress.IsFinished, Is.True);
            Assert.That(finished, Is.EqualTo(1));
            Assert.That(progress.Distance, Is.EqualTo(10f).Within(Tolerance));
        }

        [Test]
        public void A_Station_at_the_end_is_fought_before_the_Route_finishes()
        {
            var station = new Station(0, 10f);
            var progress = new RouteProgress(Straight(), new[] { station }, WalkingSpeed);

            progress.Tick(100f);

            Assert.That(progress.IsAtStation, Is.True);
            Assert.That(progress.IsFinished, Is.False);

            station.Clear();
            progress.Tick(0.1f);

            Assert.That(progress.IsFinished, Is.True);
        }

        [Test]
        public void Stations_must_be_on_the_Route_and_in_order()
        {
            Assert.Throws<ArgumentException>(() =>
                new RouteProgress(Straight(), new[] { new Station(0, 11f) }, WalkingSpeed));
            Assert.Throws<ArgumentException>(() =>
                new RouteProgress(Straight(), new[] { new Station(0, 5f), new Station(1, 2f) }, WalkingSpeed));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RouteProgress(Straight(), Array.Empty<Station>(), -1f));
        }

        [Test]
        public void Walking_does_not_allocate()
        {
            var progress = new RouteProgress(Straight(), new[] { new Station(0, 9f) }, WalkingSpeed);
            TestDelegate walk = () =>
            {
                progress.Tick(0.016f);
                _ = progress.Position;
                _ = progress.Direction;
            };

            // Run the exact measured step once first, so JIT compilation and first-call setup (of this lambda and
            // everything it calls) happen outside the measurement whatever tests ran before. It stays far from the Station.
            walk();

            Assert.That(walk, Is.Not.AllocatingGCMemory());
        }
    

        [Test]
        public void Skipping_to_a_Station_ahead_puts_the_character_there_past_the_ones_before()
        {
            var first = new Station(0, 2f);
            var second = new Station(1, 6f);
            var progress = new RouteProgress(Straight(), new[] { first, second }, WalkingSpeed);
            var reached = new List<Station>();
            progress.StationReached += reached.Add;
            progress.Tick(1f);
            Assert.That(progress.CurrentStation, Is.SameAs(first));

            Assert.That(progress.SkipTo(second), Is.True);

            Assert.That(progress.CurrentStation, Is.SameAs(second));
            Assert.That(progress.Distance, Is.EqualTo(6f).Within(Tolerance));
            Assert.That(reached, Is.EqualTo(new[] { first, second }));

            second.Clear();
            progress.Tick(1f);
            Assert.That(progress.Distance, Is.EqualTo(8f).Within(Tolerance));
        }

        [Test]
        public void Skipping_back_to_a_Station_already_passed_does_nothing()
        {
            var first = new Station(0, 2f);
            var progress = new RouteProgress(Straight(), new[] { first }, WalkingSpeed);
            first.Clear();
            progress.Tick(2f);
            progress.Tick(1f);
            var before = progress.Distance;

            Assert.That(progress.SkipTo(first), Is.False);
            Assert.That(progress.Distance, Is.EqualTo(before));
        }
    }
}
