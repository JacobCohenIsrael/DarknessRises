using DarknessRises.Game.Cameras;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Cameras
{
    public sealed class BossRoomFramingTests
    {
        private const float Pitch = 62f;
        private const float FieldOfView = 40f;
        private const float Aspect = 16f / 9f;

        private static readonly Vector3[] Positions =
        {
            new(68f, 0f, 4f),
            new(68f, 0f, 8f),
            new(61f, 0f, 8f),
            new(75f, 0f, 8f),
            new(68f, 0f, 13f),
        };

        [Test]
        public void The_centre_is_the_middle_of_the_Positions_bounds()
        {
            Assert.That(BossRoomFraming.Centre(Positions), Is.EqualTo(new Vector3(68f, 0f, 8.5f)));
        }

        [TestCase(0f)]
        [TestCase(2f)]
        public void Every_Position_with_its_margin_is_in_view_and_the_framing_is_tight(float margin)
        {
            var centre = BossRoomFraming.Centre(Positions);
            var distance = BossRoomFraming.Distance(Positions, centre, Pitch, FieldOfView, Aspect, margin, 1f);
            var camera = BossRoomFraming.CameraPosition(centre, Pitch, distance);
            var rotation = Quaternion.Inverse(FixedAngleFraming.Rotation(Pitch));
            var tanVertical = Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad);
            var tanHorizontal = tanVertical * Aspect;

            var tightest = 0f;
            foreach (var position in Positions)
            {
                var local = rotation * (position - camera);
                var horizontal = (Mathf.Abs(local.x) + margin) / (local.z * tanHorizontal);
                var vertical = (Mathf.Abs(local.y) + margin) / (local.z * tanVertical);
                Assert.That(horizontal, Is.LessThanOrEqualTo(1.0001f), $"{position} sideways");
                Assert.That(vertical, Is.LessThanOrEqualTo(1.0001f), $"{position} up or down");
                tightest = Mathf.Max(tightest, horizontal, vertical);
            }

            Assert.That(tightest, Is.EqualTo(1f).Within(0.001f), "No further back than it has to be.");
        }

        [Test]
        public void It_never_comes_closer_than_the_minimum()
        {
            var distance = BossRoomFraming.Distance(new[] { Vector3.zero }, Vector3.zero, Pitch, FieldOfView, Aspect, 0f, 12f);

            Assert.That(distance, Is.EqualTo(12f));
        }
    }
}
