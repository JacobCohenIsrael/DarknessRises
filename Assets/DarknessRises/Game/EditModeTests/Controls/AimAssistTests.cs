using DarknessRises.Game.Controls;
using DarknessRises.Game.Visibility;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Controls
{
    public sealed class AimAssistTests
    {
        private static readonly Vector3 AimPoint = new(0f, 1f, 10f);

        // Radius 2 m, halfway toward the Enemy.
        private readonly AimAssist assist = new(new AimAssistSettings(2f, 0.5f));

        [Test]
        public void ItPicksTheNearestVisibleEnemyWithinTheRadius()
        {
            var candidates = new[] { Enemy(1.5f, 10f), Enemy(0.5f, 10f), Enemy(-1f, 10f) };

            Assert.That(assist.PickTarget(AimPoint, candidates, new[] { LitSpot.WholeRoom() }), Is.EqualTo(1));
        }

        [Test]
        public void ItIgnoresEnemiesNoLightReaches()
        {
            var candidates = new[] { Enemy(0.5f, 10f), Enemy(1.5f, 10f) };

            // Only the farther one stands in a light (say a Hit spark).
            var lit = new[] { LitSpot.Around(new Vector3(1.5f, 1f, 10f), 0.5f) };

            Assert.That(assist.PickTarget(AimPoint, candidates, lit), Is.EqualTo(1));
        }

        [Test]
        public void ThereIsNoTargetInDarkness()
        {
            var candidates = new[] { Enemy(0.5f, 10f) };

            Assert.That(assist.PickTarget(AimPoint, candidates, new LitSpot[0]), Is.EqualTo(-1));
        }

        [Test]
        public void ThereIsNoTargetOutsideTheRadius()
        {
            var candidates = new[] { Enemy(2.5f, 10f), Enemy(0f, 13f) };

            Assert.That(assist.PickTarget(AimPoint, candidates, new[] { LitSpot.WholeRoom() }), Is.EqualTo(-1));
        }

        [Test]
        public void TheRadiusIsMeasuredOnTheFloorPlane()
        {
            // Far below the aim plane, but right under the finger.
            var candidates = new[] { new AimCandidate(new Vector3(0.5f, -20f, 10f), new Vector3(0.5f, -19f, 10f)) };

            Assert.That(assist.PickTarget(AimPoint, candidates, new[] { LitSpot.WholeRoom() }), Is.EqualTo(0));
        }

        [Test]
        public void TheAimBendsPartWayTowardTheTarget()
        {
            var from = new Vector3(0f, 1f, 0f);
            var target = new Vector3(10f, 0f, 10f);

            var bent = assist.Bend(from, AimPoint, target);

            // Straight ahead bent halfway toward 45 degrees right is 22.5 degrees right, at the aim point's distance and height.
            var direction = bent - from;
            Assert.That(Vector3.SignedAngle(Vector3.forward, direction, Vector3.up), Is.EqualTo(22.5f).Within(1e-3f));
            Assert.That(direction.magnitude, Is.EqualTo(10f).Within(1e-4f));
            Assert.That(bent.y, Is.EqualTo(AimPoint.y));
        }

        [Test]
        public void NoStrengthLeavesTheAimAlone()
        {
            var still = new AimAssist(new AimAssistSettings(2f, 0f));

            var bent = still.Bend(Vector3.zero, AimPoint, new Vector3(1f, 0f, 10f));

            Assert.That(Vector3.Distance(bent, AimPoint), Is.LessThan(1e-4f));
        }

        [Test]
        public void APointLightOnlyLightsWithinItsRange()
        {
            var spot = LitSpot.Around(Vector3.zero, 3f);

            Assert.That(spot.Lights(new Vector3(0f, 0f, 2.9f)), Is.True);
            Assert.That(spot.Lights(new Vector3(0f, 0f, 3.1f)), Is.False);
        }

        private static AimCandidate Enemy(float x, float z)
        {
            return new AimCandidate(new Vector3(x, 0f, z), new Vector3(x, 1f, z));
        }
    }
}
