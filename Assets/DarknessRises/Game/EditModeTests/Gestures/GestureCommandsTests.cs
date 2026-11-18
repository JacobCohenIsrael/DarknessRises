using DarknessRises.Game.Bosses;
using DarknessRises.Game.Gestures;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Gestures
{
    public sealed class GestureCommandsTests
    {
        private const float HalfAngle = 45f;

        [TestCase(GestureKind.Tap, GestureCommand.FireOnce)]
        [TestCase(GestureKind.HoldStarted, GestureCommand.FireHold)]
        [TestCase(GestureKind.HoldEnded, GestureCommand.FireRelease)]
        [TestCase(GestureKind.DoubleTap, GestureCommand.Reload)]
        [TestCase(GestureKind.TwoFingerTap, GestureCommand.None)]
        public void EachGestureStandsForItsCommand(GestureKind kind, GestureCommand command)
        {
            Assert.That(GestureCommands.For(new Gesture(kind, Vector2.zero, 0f), HalfAngle), Is.EqualTo(command));
        }

        // One-finger Flicks never trigger a Skill or Reload: up, down, sideways and diagonal all Relocate.
        [TestCase(0f, 1f)]
        [TestCase(0f, -1f)]
        [TestCase(0.3f, -1f)]
        [TestCase(1f, 0f)]
        [TestCase(-1f, 0f)]
        [TestCase(1f, 1f)]
        [TestCase(-1f, -1f)]
        public void AOneFingerFlickInAnyDirectionRelocates(float x, float y)
        {
            Assert.That(For(GestureKind.Flick, new Vector2(x, y)), Is.EqualTo(GestureCommand.Relocate));
        }

        [TestCase(0f, 1f)]
        [TestCase(0.5f, 1f)]
        [TestCase(-0.5f, 1f)]
        public void ATwoFingerFlickUpFlares(float x, float y)
        {
            Assert.That(For(GestureKind.TwoFingerFlick, new Vector2(x, y)), Is.EqualTo(GestureCommand.Flare));
        }

        [TestCase(0f, -1f)]
        [TestCase(0.5f, -1f)]
        [TestCase(-0.5f, -1f)]
        public void ATwoFingerFlickDownTriggersSlowMo(float x, float y)
        {
            Assert.That(For(GestureKind.TwoFingerFlick, new Vector2(x, y)), Is.EqualTo(GestureCommand.SlowMo));
        }

        [TestCase(1f, 0f)]
        [TestCase(-1f, 0f)]
        [TestCase(1f, 0.5f)]
        public void ATwoFingerFlickSidewaysMeansNothing(float x, float y)
        {
            Assert.That(For(GestureKind.TwoFingerFlick, new Vector2(x, y)), Is.EqualTo(GestureCommand.None));
        }

        // The mapping has no idea of the fight; Relocation only acts while the Boss fight enables it.
        [TestCase(0f, 1f, true, true)]
        [TestCase(0f, -1f, true, true)]
        [TestCase(0f, 1f, false, false)]
        [TestCase(0f, -1f, false, false)]
        public void AOneFingerFlickUpOrDownRelocatesOnlyInABossFight(float x, float y, bool bossFight, bool relocates)
        {
            var positions = new[] { new Vector3(0f, 0f, -4f), Vector3.zero, new Vector3(0f, 0f, 4f) };
            var relocation = new Relocation(positions, 1, new RelocateSettings(dashSeconds: 0.25f, maxAngleDegrees: 60f))
            {
                IsEnabled = bossFight,
            };
            var flick = new Gesture(GestureKind.Flick, Vector2.zero, 0f, new Vector2(x, y));

            var acted = GestureCommands.For(flick, HalfAngle) == GestureCommand.Relocate
                && relocation.TryRelocate(flick.Direction);

            Assert.That(acted, Is.EqualTo(relocates));
            Assert.That(relocation.IsDashing, Is.EqualTo(relocates));
        }

        private static GestureCommand For(GestureKind kind, Vector2 direction)
        {
            return GestureCommands.For(new Gesture(kind, Vector2.zero, 0f, direction.normalized), HalfAngle);
        }
    }
}
