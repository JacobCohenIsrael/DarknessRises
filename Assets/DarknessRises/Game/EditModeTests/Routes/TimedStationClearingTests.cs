using System;
using DarknessRises.Game.Routes;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Routes
{
    public sealed class TimedStationClearingTests
    {
        private static RouteProgress ProgressWithStationAt(float distance, out Station station)
        {
            station = new Station(0, distance);
            var path = new RoutePath(new[] { Vector3.zero, new Vector3(0f, 0f, 10f) });
            return new RouteProgress(path, new[] { station }, 1f);
        }

        [Test]
        public void Station_clears_after_the_hold_time()
        {
            var progress = ProgressWithStationAt(1f, out var station);
            using var clearing = new TimedStationClearing(progress, 2f);
            progress.Tick(1f);

            clearing.Tick(1.5f);
            Assert.That(station.IsCleared, Is.False);

            clearing.Tick(0.5f);
            Assert.That(station.IsCleared, Is.True);
            Assert.That(progress.IsAtStation, Is.False);
        }

        [Test]
        public void Zero_hold_time_clears_the_Station_on_arrival()
        {
            var progress = ProgressWithStationAt(1f, out var station);
            using var clearing = new TimedStationClearing(progress, 0f);

            progress.Tick(1f);

            Assert.That(station.IsCleared, Is.True);
            Assert.That(progress.IsAtStation, Is.False);
        }

        [Test]
        public void The_hold_time_only_starts_when_the_character_arrives()
        {
            var progress = ProgressWithStationAt(5f, out var station);
            using var clearing = new TimedStationClearing(progress, 1f);

            clearing.Tick(10f);
            progress.Tick(5f);

            Assert.That(station.IsCleared, Is.False);
            Assert.That(progress.IsAtStation, Is.True);
        }

        [Test]
        public void Negative_hold_time_is_rejected()
        {
            var progress = ProgressWithStationAt(1f, out _);

            Assert.Throws<ArgumentOutOfRangeException>(() => new TimedStationClearing(progress, -1f));
        }
    }
}
