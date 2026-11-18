using System;
using System.Collections.Generic;
using DarknessRises.Game.Lightning;
using DarknessRises.Game.Randomness;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Lightning
{
    public sealed class LightningAnglesTests
    {
        private static readonly LightningAngles Angles = new(23f, 33f, 10f);

        [Test]
        public void ManySeededStrikesStayWithinTheirRangesAroundTheRoomsYaw_AndVary()
        {
            var random = new SystemRandom(59);
            var yaws = new HashSet<float>();

            for (var i = 0; i < 1000; i++)
            {
                var angle = Angles.Pick(random, 172f);
                Assert.That(angle.ElevationDegrees, Is.InRange(23f, 33f));
                Assert.That(angle.YawDegrees, Is.InRange(162f, 182f));
                yaws.Add(angle.YawDegrees);
            }

            Assert.That(yaws.Count, Is.GreaterThan(900));
        }

        [Test]
        public void TheFirstAimIsTheMiddleOfTheElevationRange()
        {
            Assert.That(Angles.MiddleElevationDegrees, Is.EqualTo(28f));
        }

        [Test]
        public void NoJitterKeepsTheRoomsYaw()
        {
            var angle = new LightningAngles(28f, 28f, 0f).Pick(new SystemRandom(1), 165f);

            Assert.That(angle.ElevationDegrees, Is.EqualTo(28f));
            Assert.That(angle.YawDegrees, Is.EqualTo(165f));
        }

        [TestCase(0f, 30f, 10f)]
        [TestCase(20f, 90f, 10f)]
        [TestCase(30f, 20f, 10f)]
        [TestCase(20f, 30f, -1f)]
        public void RejectsImpossibleRanges(float minElevation, float maxElevation, float yawJitter)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new LightningAngles(minElevation, maxElevation, yawJitter));
        }
    }
}
