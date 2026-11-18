using System;
using DarknessRises.Game.Grenades;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Grenades
{
    public sealed class GrenadeKnockbackTests
    {
        private const float Radius = 3.5f;
        private const float Centre = 0.8f;
        private const float Edge = 0.2f;
        private const float Tolerance = 1e-5f;

        [TestCase(0f, Centre)]
        [TestCase(Radius * 0.5f, (Centre + Edge) * 0.5f)]
        [TestCase(Radius, Edge)]
        public void The_push_falls_off_from_the_centre_to_the_edge(float distance, float metres)
        {
            Assert.That(GrenadeKnockback.MetresAt(distance, Radius, Centre, Edge), Is.EqualTo(metres).Within(Tolerance));
        }

        [Test]
        public void Nothing_beyond_the_blast_radius_is_pushed()
        {
            Assert.That(GrenadeKnockback.MetresAt(Radius + 0.01f, Radius, Centre, Edge), Is.EqualTo(0f));
        }

        [Test]
        public void Closer_is_pushed_harder()
        {
            Assert.That(
                GrenadeKnockback.MetresAt(1f, Radius, Centre, Edge),
                Is.GreaterThan(GrenadeKnockback.MetresAt(2f, Radius, Centre, Edge)));
        }

        [Test]
        public void Bad_values_are_refused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GrenadeKnockback.MetresAt(1f, 0f, Centre, Edge));
            Assert.Throws<ArgumentOutOfRangeException>(() => GrenadeKnockback.MetresAt(1f, Radius, -1f, Edge));
            Assert.Throws<ArgumentOutOfRangeException>(() => GrenadeKnockback.MetresAt(1f, Radius, Centre, -1f));
        }
    }
}
