using DarknessRises.Game.Cameras;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Cameras
{
    public sealed class FixedAngleFramingTests
    {
        private const float Tolerance = 1e-4f;

        [TestCase(30f, 10f)]
        [TestCase(60f, 16f)]
        [TestCase(90f, 5f)]
        public void Camera_sits_the_authored_distance_from_the_character(float pitch, float distance)
        {
            var offset = FixedAngleFraming.FollowOffset(pitch, distance);

            Assert.That(offset.magnitude, Is.EqualTo(distance).Within(Tolerance));
        }

        [TestCase(30f)]
        [TestCase(60f)]
        [TestCase(75f)]
        [TestCase(90f)]
        public void Camera_looks_straight_at_the_character(float pitch)
        {
            var character = new Vector3(3f, 0f, -7f);
            var cameraPosition = character + FixedAngleFraming.FollowOffset(pitch, 12f);
            var viewDirection = FixedAngleFraming.Rotation(pitch) * Vector3.forward;

            var towardCharacter = (character - cameraPosition).normalized;

            Assert.That(Vector3.Angle(viewDirection, towardCharacter), Is.LessThan(0.01f));
        }

        [Test]
        public void Tilted_camera_sits_above_and_behind_the_character()
        {
            var offset = FixedAngleFraming.FollowOffset(60f, 16f);

            Assert.That(offset.y, Is.GreaterThan(0f), "above");
            Assert.That(offset.z, Is.LessThan(0f), "behind, so screen up is world forward");
            Assert.That(offset.x, Is.EqualTo(0f).Within(Tolerance), "never off to the side");
        }

        [Test]
        public void Straight_down_pitch_puts_the_camera_directly_overhead()
        {
            var offset = FixedAngleFraming.FollowOffset(90f, 10f);

            Assert.That(offset.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(offset.y, Is.EqualTo(10f).Within(Tolerance));
            Assert.That(offset.z, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Steeper_pitch_raises_the_camera_and_pulls_it_in_over_the_character()
        {
            var shallow = FixedAngleFraming.FollowOffset(45f, 16f);
            var steep = FixedAngleFraming.FollowOffset(70f, 16f);

            Assert.That(steep.y, Is.GreaterThan(shallow.y));
            Assert.That(Mathf.Abs(steep.z), Is.LessThan(Mathf.Abs(shallow.z)));
        }
    }
}
