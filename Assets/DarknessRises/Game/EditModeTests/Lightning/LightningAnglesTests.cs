using System;
using System.Collections.Generic;
using DarknessRises.Game.Lightning;
using DarknessRises.Game.Randomness;
using NUnit.Framework;
using UnityEngine;

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

        [Test]
        public void SomeStrikesComeInLow_AboutAsOftenAsAuthored()
        {
            var angles = new LightningAngles(18f, 46f, 28f, 0.25f, 8f, 14f);
            var random = new SystemRandom(101);
            var low = 0;

            for (var i = 0; i < 2000; i++)
            {
                var elevation = angles.Pick(random, 180f).ElevationDegrees;
                Assert.That(elevation is >= 8f and <= 14f || elevation is >= 18f and <= 46f, Is.True, $"{elevation}");
                low += elevation <= 14f ? 1 : 0;
            }

            Assert.That(low / 2000f, Is.InRange(0.2f, 0.3f));
        }

        [Test]
        public void ACertainLowChanceAlwaysComesInLow()
        {
            var angles = new LightningAngles(18f, 46f, 28f, 1f, 8f, 14f);
            var random = new SystemRandom(5);

            for (var i = 0; i < 200; i++)
            {
                Assert.That(angles.Pick(random, 180f).ElevationDegrees, Is.InRange(8f, 14f));
            }
        }

        [Test]
        public void ALowStrikeShinesBrighterSoTheFloorIsLitAsWell_UpToTheAuthoredMost()
        {
            Assert.That(new LightningAngle(32f, 0f).FloorBrightnessBoost(32f, 3f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(new LightningAngle(45f, 0f).FloorBrightnessBoost(32f, 3f), Is.EqualTo(1f));
            Assert.That(
                new LightningAngle(16f, 0f).FloorBrightnessBoost(32f, 3f),
                Is.EqualTo(Mathf.Sin(32f * Mathf.Deg2Rad) / Mathf.Sin(16f * Mathf.Deg2Rad)).Within(0.0001f));
            Assert.That(new LightningAngle(5f, 0f).FloorBrightnessBoost(32f, 3f), Is.EqualTo(3f));
        }

        [TestCase(-0.1f, 8f, 14f)]
        [TestCase(1.1f, 8f, 14f)]
        [TestCase(0.2f, 0f, 14f)]
        [TestCase(0.2f, 14f, 8f)]
        public void RejectsImpossibleLowStrikes(float lowChance, float minLow, float maxLow)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new LightningAngles(18f, 46f, 28f, lowChance, minLow, maxLow));
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
