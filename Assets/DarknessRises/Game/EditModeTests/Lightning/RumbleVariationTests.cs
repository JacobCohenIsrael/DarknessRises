using System;
using System.Collections.Generic;
using DarknessRises.Game.Lightning;
using DarknessRises.Game.Randomness;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Lightning
{
    public sealed class RumbleVariationTests
    {
        private static readonly RumbleVariation Variation = new(0.85f, 1.15f, 0.4f, 24f);

        [Test]
        public void ManySeededPicksStayWithinTheirRangesAndVary()
        {
            var random = new SystemRandom(54);
            var pitches = new HashSet<float>();

            for (var i = 0; i < 1000; i++)
            {
                var take = Variation.Pick(random);
                Assert.That(take.Pitch, Is.InRange(0.85f, 1.15f));
                Assert.That(take.ClipStartSeconds, Is.InRange(0.4f, 24f));
                pitches.Add(take.Pitch);
            }

            Assert.That(pitches.Count, Is.GreaterThan(900));
        }

        [Test]
        public void TheSameSeedPicksTheSameRumbles()
        {
            var first = new SystemRandom(3);
            var second = new SystemRandom(3);

            for (var i = 0; i < 10; i++)
            {
                var a = Variation.Pick(first);
                var b = Variation.Pick(second);
                Assert.That(a.Pitch, Is.EqualTo(b.Pitch));
                Assert.That(a.ClipStartSeconds, Is.EqualTo(b.ClipStartSeconds));
            }
        }

        [TestCase(0f, 1f, 0f, 1f)]
        [TestCase(1.2f, 1f, 0f, 1f)]
        [TestCase(1f, 1f, -1f, 1f)]
        [TestCase(1f, 1f, 2f, 1f)]
        public void RejectsImpossibleRanges(float minPitch, float maxPitch, float minStart, float maxStart)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new RumbleVariation(minPitch, maxPitch, minStart, maxStart));
        }
    }
}
