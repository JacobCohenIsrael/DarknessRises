using System;
using DarknessRises.Game.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DarknessRises.Game.EditModeTests.Player
{
    public sealed class CharacterFacingTests
    {
        private const float Tolerance = 1e-3f;
        private const float BodyTurnSpeed = 90f;
        private const float UpperBodyTurnSpeed = 360f;
        private const float MaxTwist = 100f;

        private static readonly Vector3 North = Vector3.forward;
        private static readonly Vector3 East = Vector3.right;
        private static readonly Vector3 South = Vector3.back;
        private static readonly Vector3 West = Vector3.left;

        private static CharacterFacing FacingNorth() => new(BodyTurnSpeed, UpperBodyTurnSpeed, MaxTwist, 0f);

        [Test]
        public void Walking_and_aiming_ahead_faces_straight_ahead()
        {
            var facing = FacingNorth();

            facing.Tick(1f, North, North);

            Assert.That(facing.BodyYaw, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(facing.UpperBodyYaw, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(facing.Twist, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Upper_body_turns_to_the_aim_while_the_legs_keep_walking_the_Route()
        {
            var facing = FacingNorth();

            facing.Tick(1f, North, East);

            Assert.That(facing.UpperBodyYaw, Is.EqualTo(90f).Within(Tolerance));
            Assert.That(facing.BodyYaw, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(facing.Twist, Is.EqualTo(90f).Within(Tolerance));
        }

        [Test]
        public void Upper_body_turns_toward_the_aim_at_its_turn_speed()
        {
            var facing = FacingNorth();

            facing.Tick(0.125f, North, East);

            Assert.That(facing.UpperBodyYaw, Is.EqualTo(45f).Within(Tolerance));
        }

        [Test]
        public void Aiming_behind_still_faces_the_aim_with_the_legs_turned_as_little_as_the_twist_allows()
        {
            var facing = FacingNorth();

            facing.Tick(1f, North, South);

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(facing.UpperBodyYaw, 180f)), Is.LessThan(Tolerance));
            Assert.That(Mathf.Abs(facing.Twist), Is.EqualTo(MaxTwist).Within(Tolerance));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(facing.BodyYaw, 0f)), Is.EqualTo(180f - MaxTwist).Within(Tolerance));
        }

        [Test]
        public void Legs_turn_the_long_way_round_rather_than_twist_the_upper_body_too_far()
        {
            // Aiming back over the left shoulder (yaw -150) while walking north, the legs sit at the twist limit.
            var aim = new Vector3(-0.5f, 0f, -Mathf.Sqrt(3f) / 2f);
            var facing = FacingNorth();
            facing.Tick(1f, North, aim);
            Assert.That(facing.BodyYaw, Is.EqualTo(-50f).Within(Tolerance));

            // The Route turns east. The short way there would twist the upper body past its limit,
            // so the legs swing round through facing the aim and stop as close to east as the limit allows.
            facing.Tick(10f, East, aim);

            Assert.That(facing.BodyYaw, Is.EqualTo(110f).Within(Tolerance));
            Assert.That(facing.Twist, Is.EqualTo(MaxTwist).Within(Tolerance));
        }

        [Test]
        public void Legs_turn_toward_a_new_Route_direction_at_their_turn_speed()
        {
            var facing = FacingNorth();

            facing.Tick(0.5f, East, East);

            Assert.That(facing.BodyYaw, Is.EqualTo(45f).Within(Tolerance));

            facing.Tick(1f, East, East);

            Assert.That(facing.BodyYaw, Is.EqualTo(90f).Within(Tolerance));
        }

        [Test]
        public void Stopped_legs_turn_to_face_the_aim()
        {
            var facing = FacingNorth();

            facing.Tick(0.5f, Vector3.zero, West);

            Assert.That(facing.UpperBodyYaw, Is.EqualTo(-90f).Within(Tolerance));
            Assert.That(facing.BodyYaw, Is.EqualTo(-45f).Within(Tolerance));

            facing.Tick(1f, Vector3.zero, West);

            Assert.That(facing.BodyYaw, Is.EqualTo(-90f).Within(Tolerance));
            Assert.That(facing.Twist, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Without_an_aim_the_upper_body_keeps_its_last_aim()
        {
            var facing = FacingNorth();
            facing.Tick(1f, North, East);

            facing.Tick(1f, North, Vector3.zero);

            Assert.That(facing.UpperBodyYaw, Is.EqualTo(90f).Within(Tolerance));
        }

        [Test]
        public void Walking_with_the_legs_turned_away_reads_as_a_strafe_in_body_space()
        {
            var facing = FacingNorth();
            Assert.That(Vector2.Distance(facing.ToBodySpace(North), Vector2.up), Is.LessThan(Tolerance));

            // Stopped, the legs turn east to face the aim; walking north is then a step to their left.
            facing.Tick(1f, Vector3.zero, East);

            Assert.That(Vector2.Distance(facing.ToBodySpace(North), Vector2.left), Is.LessThan(Tolerance));
            Assert.That(Vector2.Distance(facing.ToBodySpace(South), Vector2.right), Is.LessThan(Tolerance));
            Assert.That(Vector2.Distance(facing.ToBodySpace(West), Vector2.down), Is.LessThan(Tolerance));
        }

        [Test]
        public void Rejects_a_twist_limit_outside_half_a_turn()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterFacing(BodyTurnSpeed, UpperBodyTurnSpeed, 181f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterFacing(BodyTurnSpeed, UpperBodyTurnSpeed, -1f, 0f));
        }

        [Test]
        public void Turning_does_not_allocate()
        {
            var facing = FacingNorth();

            Assert.That(() =>
            {
                facing.Tick(0.016f, North, East);
                facing.ToBodySpace(North);
            }, Is.Not.AllocatingGCMemory());
        }
    }
}
