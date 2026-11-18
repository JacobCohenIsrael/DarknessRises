using DarknessRises.Game.Gestures;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Gestures
{
    public sealed class GestureCommandsTests
    {
        private const float HalfAngle = 30f;

        [TestCase(GestureKind.Tap, GestureCommand.FireOnce)]
        [TestCase(GestureKind.HoldStarted, GestureCommand.FireHold)]
        [TestCase(GestureKind.HoldEnded, GestureCommand.FireRelease)]
        [TestCase(GestureKind.DoubleTap, GestureCommand.None)]
        [TestCase(GestureKind.TwoFingerTap, GestureCommand.SlowMo)]
        public void EachGestureStandsForItsCommand(GestureKind kind, GestureCommand command)
        {
            Assert.That(GestureCommands.For(new Gesture(kind, Vector2.zero, 0f), HalfAngle), Is.EqualTo(command));
        }

        // The mapping has no idea of the fight: a Flick down reloads in a Boss fight as much as anywhere.
        [TestCase(0f, -1f)]
        [TestCase(0.3f, -1f)]
        [TestCase(-0.3f, -1f)]
        public void AFlickDownReloadsEvenInABossFight(float x, float y)
        {
            Assert.That(For(new Vector2(x, y)), Is.EqualTo(GestureCommand.Reload));
        }

        [TestCase(0f, 1f)]
        [TestCase(0.3f, 1f)]
        public void AFlickUpFlares(float x, float y)
        {
            Assert.That(For(new Vector2(x, y)), Is.EqualTo(GestureCommand.Flare));
        }

        [TestCase(1f, 0f)]
        [TestCase(-1f, 0f)]
        [TestCase(1f, 1f)]
        [TestCase(-1f, -1f)]
        public void AFlickSidewaysOrDiagonallyRelocates(float x, float y)
        {
            Assert.That(For(new Vector2(x, y)), Is.EqualTo(GestureCommand.Relocate));
        }

        private static GestureCommand For(Vector2 direction)
        {
            return GestureCommands.For(new Gesture(GestureKind.Flick, Vector2.zero, 0f, direction.normalized), HalfAngle);
        }
    }
}
