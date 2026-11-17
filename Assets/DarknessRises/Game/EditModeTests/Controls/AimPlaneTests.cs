using DarknessRises.Game.Controls;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Controls
{
    public sealed class AimPlaneTests
    {
        [Test]
        public void APointerRayMeetsThePlaneAtTheWeaponsHeight()
        {
            var ray = new Ray(new Vector3(1f, 11f, -4f), new Vector3(0f, -1f, 1f));

            Assert.That(AimPlane.TryIntersect(ray, 1f, out var point), Is.True);

            Assert.That(point.x, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(point.y, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(point.z, Is.EqualTo(6f).Within(1e-4f));
        }

        [Test]
        public void ARayPointingAwayFromThePlaneMissesIt()
        {
            var ray = new Ray(new Vector3(0f, 10f, 0f), Vector3.up);

            Assert.That(AimPlane.TryIntersect(ray, 1f, out _), Is.False);
        }

        [Test]
        public void AHorizontalRayMissesThePlane()
        {
            var ray = new Ray(new Vector3(0f, 10f, 0f), Vector3.forward);

            Assert.That(AimPlane.TryIntersect(ray, 1f, out _), Is.False);
        }

        [Test]
        public void TheAimDirectionIsFlat()
        {
            Assert.That(AimPlane.TryGetFlatDirection(new Vector3(0f, 1f, 0f), new Vector3(3f, 5f, 4f), out var direction), Is.True);

            Assert.That(direction.x, Is.EqualTo(0.6f).Within(1e-5f));
            Assert.That(direction.y, Is.EqualTo(0f));
            Assert.That(direction.z, Is.EqualTo(0.8f).Within(1e-5f));
        }

        [Test]
        public void AimingStraightDownHasNoDirection()
        {
            Assert.That(AimPlane.TryGetFlatDirection(Vector3.up, Vector3.zero, out _), Is.False);
        }
    }
}
