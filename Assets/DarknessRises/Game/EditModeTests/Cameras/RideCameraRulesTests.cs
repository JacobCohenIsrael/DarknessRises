using System;
using DarknessRises.Game.Cameras;
using DarknessRises.Game.Routes;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Cameras
{
    /// <summary>The rules behind the Ride camera: its swing, the heading it reads off the Route, and the Station framing.</summary>
    public sealed class RideCameraRulesTests
    {
        private const float Tolerance = 1e-4f;
        private const float MaxSwing = 40f;

        // 10 m east, then 10 m north.
        private static RoutePath EastThenNorth() => new(new[]
        {
            Vector3.zero,
            new Vector3(10f, 0f, 0f),
            new Vector3(10f, 0f, 10f),
        });

        [Test]
        public void A_swung_camera_still_looks_straight_at_its_point()
        {
            var point = new Vector3(3f, 0f, -7f);
            var cameraPosition = point + FixedAngleFraming.FollowOffset(46f, 38f, 12f);
            var viewDirection = FixedAngleFraming.Rotation(46f, 38f) * Vector3.forward;

            Assert.That(Vector3.Angle(viewDirection, (point - cameraPosition).normalized), Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(cameraPosition, point), Is.EqualTo(12f).Within(Tolerance));
        }

        [Test]
        public void A_camera_swung_east_stands_south_west_of_its_point_at_the_same_height()
        {
            var straight = FixedAngleFraming.FollowOffset(46f, 12f);
            var swung = FixedAngleFraming.FollowOffset(46f, 38f, 12f);

            Assert.That(swung.x, Is.LessThan(0f), "west");
            Assert.That(swung.z, Is.LessThan(0f), "south, where the Rooms' walls are cut down");
            Assert.That(swung.y, Is.EqualTo(straight.y).Within(Tolerance));
        }

        [Test]
        public void No_swing_is_the_north_looking_camera()
        {
            Assert.That(
                Vector3.Distance(FixedAngleFraming.FollowOffset(60f, 0f, 16f), FixedAngleFraming.FollowOffset(60f, 16f)),
                Is.LessThan(Tolerance));
            Assert.That(Quaternion.Angle(FixedAngleFraming.Rotation(60f, 0f), FixedAngleFraming.Rotation(60f)), Is.LessThan(0.01f));
        }

        [Test]
        public void The_camera_swings_its_most_for_a_Route_heading_east_or_west()
        {
            Assert.That(RideSwing.TowardsDegrees(Vector3.right, MaxSwing), Is.EqualTo(MaxSwing).Within(Tolerance));
            Assert.That(RideSwing.TowardsDegrees(Vector3.left, MaxSwing), Is.EqualTo(-MaxSwing).Within(Tolerance));
        }

        [Test]
        public void The_camera_looks_north_for_a_Route_heading_north_or_south()
        {
            Assert.That(RideSwing.TowardsDegrees(Vector3.forward, MaxSwing), Is.EqualTo(0f).Within(Tolerance));
            Assert.That(RideSwing.TowardsDegrees(Vector3.back, MaxSwing), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void The_swing_changes_smoothly_with_the_heading_and_ignores_its_length()
        {
            var northEast = RideSwing.TowardsDegrees(new Vector3(5f, 0f, 5f), MaxSwing);
            var southEast = RideSwing.TowardsDegrees(new Vector3(0.1f, 0f, -0.1f), MaxSwing);

            Assert.That(northEast, Is.EqualTo(MaxSwing * Mathf.Sqrt(0.5f)).Within(Tolerance));
            Assert.That(southEast, Is.EqualTo(northEast).Within(Tolerance), "No jump as the Route turns through south.");
        }

        [Test]
        public void A_heading_with_no_flat_length_swings_nothing()
        {
            Assert.That(RideSwing.TowardsDegrees(Vector3.zero, MaxSwing), Is.Zero);
            Assert.That(RideSwing.TowardsDegrees(Vector3.up, MaxSwing), Is.Zero);
        }

        [Test]
        public void The_swing_eases_towards_its_target_and_never_overshoots()
        {
            var swing = new RideSwing(1f);

            swing.Tick(1f, 40f);
            Assert.That(swing.Degrees, Is.EqualTo(40f * (1f - Mathf.Exp(-1f))).Within(Tolerance));

            for (var i = 0; i < 200; i++)
            {
                swing.Tick(0.1f, 40f);
                Assert.That(swing.Degrees, Is.LessThanOrEqualTo(40f));
            }

            Assert.That(swing.Degrees, Is.EqualTo(40f).Within(1e-3f));
        }

        [Test]
        public void The_swing_is_the_same_however_the_time_is_cut_into_steps()
        {
            var oneStep = new RideSwing(0.8f);
            var manySteps = new RideSwing(0.8f);

            oneStep.Tick(0.5f, 30f);
            for (var i = 0; i < 50; i++)
            {
                manySteps.Tick(0.01f, 30f);
            }

            Assert.That(manySteps.Degrees, Is.EqualTo(oneStep.Degrees).Within(1e-3f));
        }

        [Test]
        public void The_swing_snaps_when_told_to_or_when_it_has_no_easing()
        {
            var eased = new RideSwing(1f);
            eased.Snap(25f);
            Assert.That(eased.Degrees, Is.EqualTo(25f));

            var instant = new RideSwing(0f);
            instant.Tick(0.001f, -12f);
            Assert.That(instant.Degrees, Is.EqualTo(-12f));
        }

        [Test]
        public void Easing_the_swing_does_not_allocate()
        {
            var swing = new RideSwing(1f);

            AllocationAssert.DoesNotAllocate(() => swing.Tick(0.016f, RideSwing.TowardsDegrees(Vector3.right, MaxSwing)));
        }

        [Test]
        public void Easing_cant_take_less_than_no_time()
        {
            Assert.That(() => new RideSwing(-1f), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void On_a_straight_leg_the_heading_is_the_legs_direction()
        {
            var heading = RouteHeading.Ahead(EastThenNorth(), 1f, 4f);

            Assert.That(Vector3.Distance(heading, Vector3.right), Is.LessThan(Tolerance));
        }

        [Test]
        public void The_heading_starts_turning_before_the_corner()
        {
            // From 6 m along, 8 m ahead is 4 m up the north leg: the chord cuts the corner.
            var heading = RouteHeading.Ahead(EastThenNorth(), 6f, 8f);

            Assert.That(heading.x, Is.GreaterThan(0f));
            Assert.That(heading.z, Is.GreaterThan(0f));
            Assert.That(heading.magnitude, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(Vector3.Distance(heading, new Vector3(4f, 0f, 4f).normalized), Is.LessThan(Tolerance));
        }

        [Test]
        public void At_the_end_of_the_Route_the_heading_is_the_last_legs()
        {
            var heading = RouteHeading.Ahead(EastThenNorth(), 20f, 8f);

            Assert.That(Vector3.Distance(heading, Vector3.forward), Is.LessThan(Tolerance));
        }

        [Test]
        public void Reading_no_distance_ahead_gives_the_leg_underfoot()
        {
            var heading = RouteHeading.Ahead(EastThenNorth(), 3f, 0f);

            Assert.That(Vector3.Distance(heading, Vector3.right), Is.LessThan(Tolerance));
        }

        [Test]
        public void The_heading_is_flat_on_a_Route_that_climbs()
        {
            var ramp = new RoutePath(new[] { Vector3.zero, new Vector3(3f, 4f, 0f) });

            var heading = RouteHeading.Ahead(ramp, 0f, 2f);

            Assert.That(Vector3.Distance(heading, Vector3.right), Is.LessThan(Tolerance));
        }

        [Test]
        public void Reading_the_heading_does_not_allocate()
        {
            var path = EastThenNorth();

            AllocationAssert.DoesNotAllocate(() => RouteHeading.Ahead(path, 6f, 8f));
        }

        [TestCase(StationBeat.Walking, false)]
        [TestCase(StationBeat.Ready, true)]
        [TestCase(StationBeat.Fight, true)]
        [TestCase(StationBeat.Clear, true)]
        [TestCase(StationBeat.Quiet, true)]
        [TestCase(StationBeat.Go, false)]
        public void The_Station_framing_holds_from_the_plant_until_the_Go_cue(StationBeat beat, bool held)
        {
            Assert.That(StationFraming.IsHeldIn(beat), Is.EqualTo(held));
        }

        [Test]
        public void The_Station_framing_looks_a_share_of_the_way_towards_the_fight()
        {
            var focus = StationFraming.Focus(new Vector3(2f, 0f, 2f), new Vector3(10f, 0f, 2f), 0.25f, 5f);

            Assert.That(Vector3.Distance(focus, new Vector3(4f, 0f, 2f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void The_Station_framing_never_looks_far_from_the_character()
        {
            var focus = StationFraming.Focus(Vector3.zero, new Vector3(0f, 0f, 40f), 0.5f, 3f);

            Assert.That(Vector3.Distance(focus, new Vector3(0f, 0f, 3f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void With_no_fight_centre_to_look_at_the_Station_framing_centres_the_character()
        {
            var station = new Vector3(7f, 0f, 6.5f);

            Assert.That(StationFraming.Focus(station, station, 0.25f, 3f), Is.EqualTo(station));
            Assert.That(StationFraming.Focus(station, new Vector3(20f, 0f, 9f), 0f, 3f), Is.EqualTo(station));
        }

        [Test]
        public void The_Station_framing_stays_on_the_floor()
        {
            var focus = StationFraming.Focus(Vector3.zero, new Vector3(4f, 3f, 0f), 0.5f, 10f);

            Assert.That(focus.y, Is.Zero);
        }

        [Test]
        public void Look_ahead_leads_a_stopped_character_when_told_to()
        {
            var path = new RoutePath(new[] { Vector3.zero, new Vector3(0f, 0f, 20f) });
            var station = new Station(0, 5f);
            var progress = new RouteProgress(path, new[] { station }, 1f);
            var lookAhead = new RouteLookAhead(4f, 0f, 2f);
            progress.Tick(5f);
            Assert.That(progress.IsAtStation, Is.True);

            // Held at the Station, the camera eases back onto the character.
            lookAhead.Tick(10f, progress, leading: false);
            Assert.That(lookAhead.Distance, Is.Zero);

            // On the Go cue it leads again, while the character still stands.
            lookAhead.Tick(1f, progress, leading: true);
            Assert.That(lookAhead.Distance, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(Vector3.Distance(lookAhead.Target, new Vector3(0f, 0f, 7f)), Is.LessThan(Tolerance));
        }
    }
}
