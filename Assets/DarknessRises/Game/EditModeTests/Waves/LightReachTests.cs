using DarknessRises.Game.Waves;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Waves
{
    public sealed class LightReachTests
    {
        private static readonly Vector3 Origin = Vector3.zero;

        [Test]
        public void Directional_light_covers_the_whole_Room()
        {
            Assert.That(LightReach.Covers(LightType.Directional, Origin, Vector3.down, 0f, 0f, new Vector3(500f, 0f, -500f)), Is.True);
        }

        [Test]
        public void Point_light_covers_its_range_and_no_further()
        {
            Assert.That(LightReach.Covers(LightType.Point, Origin, Vector3.forward, 4f, 0f, new Vector3(0f, 0f, 3.9f)), Is.True);
            Assert.That(LightReach.Covers(LightType.Point, Origin, Vector3.forward, 4f, 0f, new Vector3(3f, 0f, 3f)), Is.False);
        }

        [Test]
        public void Spot_light_covers_only_inside_its_cone_and_range()
        {
            const float range = 10f;
            const float spotAngle = 60f;

            Assert.That(LightReach.Covers(LightType.Spot, Origin, Vector3.forward, range, spotAngle, new Vector3(1f, 0f, 5f)), Is.True);
            Assert.That(LightReach.Covers(LightType.Spot, Origin, Vector3.forward, range, spotAngle, new Vector3(5f, 0f, 5f)), Is.False, "Outside the cone.");
            Assert.That(LightReach.Covers(LightType.Spot, Origin, Vector3.forward, range, spotAngle, new Vector3(0f, 0f, 11f)), Is.False, "Beyond its range.");
            Assert.That(LightReach.Covers(LightType.Spot, Origin, Vector3.forward, range, spotAngle, new Vector3(0f, 0f, -2f)), Is.False, "Behind it.");
        }
    }
}
