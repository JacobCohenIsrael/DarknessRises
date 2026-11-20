using System;
using DarknessRises.Game.Routes;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Routes
{
    public sealed class RouteRejoinTests
    {
        private const float Tolerance = 1e-4f;
        private const float OnRoute = 0.25f;
        private const float Sample = 0.5f;
        private const float WalkingSpeed = 3f;
        private const float FrameSeconds = 1f / 60f;

        // East 10 m, then north 10 m: the Boss Station at 5 m, the next Station (the far door's calm) at 15 m.
        private static RoutePath EastThenNorth() => new(new[] { Vector3.zero, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f) });

        private static readonly Vector3 BossStation = new(5f, 0f, 0f);

        // Where Relocate left the character: north of the Boss Station, off the Route.
        private static readonly Vector3 OffRoute = new(5f, 0f, 6f);

        [Test]
        public void Character_still_on_the_Station_rejoins_where_it_left()
        {
            var joinAt = RouteRejoin.JoinDistance(BossStation, EastThenNorth(), 5f, 15f, OnRoute, Sample, (_, _) => true);

            Assert.That(joinAt, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void Character_a_little_off_the_Station_still_counts_as_on_it()
        {
            var nearly = BossStation + new Vector3(0.1f, 0.5f, -0.1f);

            var joinAt = RouteRejoin.JoinDistance(nearly, EastThenNorth(), 5f, 15f, OnRoute, Sample, (_, _) => true);

            Assert.That(joinAt, Is.EqualTo(5f).Within(Tolerance), "height doesn't count");
        }

        [Test]
        public void Character_off_the_Route_heads_for_the_farthest_point_it_can_walk_straight_to()
        {
            var joinAt = RouteRejoin.JoinDistance(OffRoute, EastThenNorth(), 5f, 15f, OnRoute, Sample, (_, _) => true);

            Assert.That(joinAt, Is.EqualTo(15f).Within(Tolerance), "straight for the next stop when nothing is in the way");
        }

        [Test]
        public void An_Obstacle_in_the_way_keeps_the_join_on_this_side_of_it()
        {
            // A wall along x = 8 blocks every walk that crosses it.
            static bool Clear(Vector3 from, Vector3 to) => (from.x - 8f) * (to.x - 8f) > 0f;

            var joinAt = RouteRejoin.JoinDistance(OffRoute, EastThenNorth(), 5f, 15f, OnRoute, Sample, Clear);

            Assert.That(joinAt, Is.EqualTo(7.5f).Within(Tolerance), "the farthest sample short of the wall");
        }

        [Test]
        public void Nowhere_clear_falls_back_to_where_it_left_the_Route()
        {
            var joinAt = RouteRejoin.JoinDistance(OffRoute, EastThenNorth(), 5f, 15f, OnRoute, Sample, (_, _) => false);

            Assert.That(joinAt, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void No_check_counts_every_walk_as_clear()
        {
            var joinAt = RouteRejoin.JoinDistance(OffRoute, EastThenNorth(), 5f, 15f, OnRoute, Sample, null);

            Assert.That(joinAt, Is.EqualTo(15f).Within(Tolerance));
        }

        [Test]
        public void The_join_lies_between_where_it_left_and_the_next_stop([Values(0.3f, 0.7f, 3f)] float sample)
        {
            var blockedPastHalfway = new Func<Vector3, Vector3, bool>((_, to) => to.z < 4f);

            var joinAt = RouteRejoin.JoinDistance(OffRoute, EastThenNorth(), 5f, 15f, OnRoute, sample, blockedPastHalfway);

            Assert.That(joinAt, Is.InRange(5f, 15f));
            Assert.That(EastThenNorth().PositionAt(joinAt).z, Is.LessThan(4f));
        }

        [Test]
        public void Rejoining_walks_from_where_the_character_stands_onto_the_Route_without_a_jump()
        {
            var path = EastThenNorth();
            var bossStation = new Station(0, 5f);
            var farDoor = new Station(1, 15f);
            var progress = new RouteProgress(path, new[] { bossStation, farDoor }, WalkingSpeed, new WalkEasing(1.5f, 0.6f, 0.35f));
            progress.Tick(10f);
            Assert.That(progress.CurrentStation, Is.SameAs(bossStation));

            var joinAt = RouteRejoin.JoinDistance(OffRoute, path, progress.Distance, progress.NextStopDistance, OnRoute, Sample, null);
            progress.Rejoin(OffRoute, joinAt);

            Assert.That(Vector3.Distance(progress.Position, OffRoute), Is.LessThan(Tolerance), "starts where it stands");
            progress.Tick(1f);
            Assert.That(Vector3.Distance(progress.Position, OffRoute), Is.LessThan(Tolerance), "waits for the Station to clear");

            bossStation.Clear();
            var previous = progress.Position;
            var largestStep = 0f;
            for (var frame = 0; frame < 1000 && !progress.IsAtStation; frame++)
            {
                progress.Tick(FrameSeconds);
                largestStep = Mathf.Max(largestStep, Vector3.Distance(previous, progress.Position));
                previous = progress.Position;
            }

            Assert.That(progress.CurrentStation, Is.SameAs(farDoor), "walks on to the far door");
            Assert.That(Vector3.Distance(progress.Position, path.PositionAt(15f)), Is.LessThan(Tolerance), "ends on the Route");
            Assert.That(largestStep, Is.LessThanOrEqualTo(WalkingSpeed * FrameSeconds + Tolerance), "never faster than walking");
        }
    }
}
