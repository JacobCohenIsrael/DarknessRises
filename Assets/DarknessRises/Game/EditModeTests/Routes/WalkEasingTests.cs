using System;
using DarknessRises.Game.Routes;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Routes
{
    public sealed class WalkEasingTests
    {
        private const float Tolerance = 1e-4f;
        private const float WalkingSpeed = 2f;

        // Slows over the last 2 m before a Station, picks up over 1 s after one, never slower than a quarter pace.
        private static readonly WalkEasing Easing = new(2f, 1f, 0.25f);

        // A straight 20 m Route north.
        private static RoutePath Straight() => new(new[] { Vector3.zero, new Vector3(0f, 0f, 20f) });

        private static void Walk(RouteProgress progress, float seconds, float step = 0.01f)
        {
            for (var t = 0f; t < seconds; t += step)
            {
                progress.Tick(step);
            }
        }

        [Test]
        public void Full_pace_away_from_any_Station()
        {
            Assert.That(Easing.SpeedFraction(5f, 5f), Is.EqualTo(1f));
            Assert.That(Easing.SpeedFraction(float.PositiveInfinity, float.PositiveInfinity), Is.EqualTo(1f));
        }

        [Test]
        public void Slows_over_the_braking_distance_down_to_the_slowest_pace()
        {
            Assert.That(Easing.SpeedFraction(2f, 5f), Is.EqualTo(1f).Within(Tolerance));
            Assert.That(Easing.SpeedFraction(1f, 5f), Is.EqualTo(0.625f).Within(Tolerance));
            Assert.That(Easing.SpeedFraction(0f, 5f), Is.EqualTo(0.25f).Within(Tolerance));
        }

        [Test]
        public void Picks_up_from_the_slowest_pace_after_setting_off()
        {
            Assert.That(Easing.SpeedFraction(10f, 0f), Is.EqualTo(0.25f).Within(Tolerance));
            Assert.That(Easing.SpeedFraction(10f, 0.5f), Is.EqualTo(0.625f).Within(Tolerance));
            Assert.That(Easing.SpeedFraction(10f, 1f), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Setting_off_towards_a_near_Station_takes_the_slower_of_the_two()
        {
            Assert.That(Easing.SpeedFraction(1f, 0.9f), Is.EqualTo(0.625f).Within(Tolerance));
            Assert.That(Easing.SpeedFraction(1.9f, 0.5f), Is.EqualTo(0.625f).Within(Tolerance));
        }

        [Test]
        public void The_default_doesnt_ease()
        {
            Assert.That(default(WalkEasing).SpeedFraction(0f, 0f), Is.EqualTo(1f));
        }

        [Test]
        public void The_slowest_pace_must_still_move()
        {
            Assert.That(() => new WalkEasing(1f, 1f, 0f), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new WalkEasing(-1f, 1f, 0.5f), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new WalkEasing(1f, -1f, 0.5f), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Character_walks_in_at_full_pace_at_the_start_of_the_Route()
        {
            var progress = new RouteProgress(Straight(), new[] { new Station(0, 10f) }, WalkingSpeed, Easing);

            progress.Tick(1f);

            Assert.That(progress.Distance, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(progress.SpeedFraction, Is.EqualTo(1f));
        }

        [Test]
        public void Character_slows_into_a_Station_and_still_arrives()
        {
            var station = new Station(0, 10f);
            var progress = new RouteProgress(Straight(), new[] { station }, WalkingSpeed, Easing);

            Walk(progress, 5.1f);
            Assert.That(progress.IsAtStation, Is.False, "At full pace it would be there by now.");
            Assert.That(progress.SpeedFraction, Is.LessThan(1f));
            Assert.That(progress.SpeedFraction, Is.GreaterThanOrEqualTo(0.25f));

            Walk(progress, 5f);
            Assert.That(progress.CurrentStation, Is.SameAs(station));
            Assert.That(progress.Distance, Is.EqualTo(10f).Within(Tolerance));
            Assert.That(progress.SpeedFraction, Is.Zero, "It stands still at the Station.");
        }

        [Test]
        public void Character_picks_its_pace_back_up_after_leaving_a_Station()
        {
            var station = new Station(0, 4f);
            var progress = new RouteProgress(Straight(), new[] { station }, WalkingSpeed, Easing);
            Walk(progress, 6f);
            Assert.That(progress.IsAtStation, Is.True);

            station.Clear();
            progress.Tick(0.01f);
            Assert.That(progress.SpeedFraction, Is.EqualTo(0.25f).Within(0.02f));

            Walk(progress, 0.5f);
            Assert.That(progress.SpeedFraction, Is.GreaterThan(0.5f).And.LessThan(1f));

            Walk(progress, 1f);
            Assert.That(progress.SpeedFraction, Is.EqualTo(1f));
        }

        [Test]
        public void Without_easing_the_pace_is_all_or_nothing()
        {
            var station = new Station(0, 4f);
            var progress = new RouteProgress(Straight(), new[] { station }, WalkingSpeed);

            progress.Tick(1f);
            Assert.That(progress.SpeedFraction, Is.EqualTo(1f));

            progress.Tick(2f);
            Assert.That(progress.IsAtStation, Is.True);
            progress.Tick(1f);
            Assert.That(progress.SpeedFraction, Is.Zero);
        }
    }
}
