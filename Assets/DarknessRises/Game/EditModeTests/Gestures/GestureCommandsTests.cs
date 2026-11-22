using System;
using System.Collections.Generic;
using DarknessRises.Game.Bosses;
using DarknessRises.Game.Gestures;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Gestures
{
    public sealed class GestureCommandsTests
    {
        private const float HalfAngle = 45f;

        private static readonly GestureThresholds Thresholds =
            new(0.25f, 0.03f, 0.3f, 0.3f, 0.1f, 0.08f, 0.3f, 0.15f, 0.08f, 0.35f, 35f);

        private static readonly Vector2 Left = new(0.35f, 0.5f);
        private static readonly Vector2 Right = new(0.65f, 0.5f);

        [TestCase(GestureKind.Tap, GestureCommand.FireOnce)]
        [TestCase(GestureKind.HoldStarted, GestureCommand.FireHold)]
        [TestCase(GestureKind.HoldEnded, GestureCommand.FireRelease)]
        [TestCase(GestureKind.TwoFingerDoubleTap, GestureCommand.Reload)]
        [TestCase(GestureKind.TwoFingerTap, GestureCommand.None)]
        public void EachGestureStandsForItsCommand(GestureKind kind, GestureCommand command)
        {
            Assert.That(GestureCommands.For(new Gesture(kind, Vector2.zero, 0f), HalfAngle), Is.EqualTo(command));
        }

        [Test]
        public void ATwoFingerDoubleTapReloadsAndFiresNothing()
        {
            var commands = Recognize(recognizer =>
            {
                TwoFingerTap(recognizer, 0, Left, Right, 0f);
                TwoFingerTap(recognizer, 2, Left, Right, 0.25f);
            });

            Assert.That(commands, Is.EqualTo(new[] { GestureCommand.Reload }));
        }

        [Test]
        public void AOneFingerDoubleTapIsTwoShotsAndNoReload()
        {
            var commands = Recognize(recognizer =>
            {
                Touch(recognizer, 0, Left, 0f, 0.08f);
                Touch(recognizer, 1, Left, 0.15f, 0.22f);
            });

            Assert.That(commands, Is.EqualTo(new[] { GestureCommand.FireOnce, GestureCommand.FireOnce }));
        }

        [Test]
        public void OneTwoFingerTapDoesNothing()
        {
            var commands = Recognize(recognizer => TwoFingerTap(recognizer, 0, Left, Right, 0f));

            Assert.That(commands, Is.Empty);
        }

        [TestCase(0.6f, 0f)]
        [TestCase(0.25f, 0.2f)]
        public void TwoTwoFingerTapsTooFarApartInTimeOrSpaceDoNotReload(float secondStart, float offset)
        {
            var away = new Vector2(0f, offset);
            var commands = Recognize(recognizer =>
            {
                TwoFingerTap(recognizer, 0, Left, Right, 0f);
                TwoFingerTap(recognizer, 2, Left + away, Right + away, secondStart);
            });

            Assert.That(commands, Is.Empty);
        }

        [TestCase(0.2f, GestureCommand.Flare)]
        [TestCase(-0.2f, GestureCommand.Adrenaline)]
        public void ATwoFingerFlickStillTriggersItsSkillAndNothingElse(float travelY, GestureCommand command)
        {
            var travel = new Vector2(0f, travelY);
            var commands = Recognize(recognizer =>
            {
                Sample(recognizer, 0, TouchSamplePhase.Began, Left, 0f, 1);
                Sample(recognizer, 1, TouchSamplePhase.Began, Right, 0.03f, 2);
                Sample(recognizer, 0, TouchSamplePhase.Ended, Left + travel, 0.15f, 2);
                Sample(recognizer, 1, TouchSamplePhase.Ended, Right + travel, 0.15f, 1);
            });

            Assert.That(commands, Is.EqualTo(new[] { command }));
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
        public void ATwoFingerFlickDownTriggersAdrenaline(float x, float y)
        {
            Assert.That(For(GestureKind.TwoFingerFlick, new Vector2(x, y)), Is.EqualTo(GestureCommand.Adrenaline));
        }

        // Left or right throws the Lure that way, within the same half-angle of the horizontal as up and down have.
        [TestCase(1f, 0f)]
        [TestCase(-1f, 0f)]
        [TestCase(1f, 0.5f)]
        [TestCase(-1f, -0.6f)]
        public void ATwoFingerFlickSidewaysThrowsTheLure(float x, float y)
        {
            Assert.That(For(GestureKind.TwoFingerFlick, new Vector2(x, y)), Is.EqualTo(GestureCommand.Lure));
        }

        [Test]
        public void AStillTwoFingerFlickMeansNothing()
        {
            Assert.That(For(GestureKind.TwoFingerFlick, Vector2.zero), Is.EqualTo(GestureCommand.None));
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

        // The commands the core recognizers produce for the touches, ignoring the ones that mean nothing.
        private static List<GestureCommand> Recognize(Action<GestureRecognizer> touches)
        {
            var sink = new CommandRecorder();
            touches(GestureRecognizer.WithCoreGestures(Thresholds, sink));
            return sink.Commands;
        }

        private static void TwoFingerTap(GestureRecognizer recognizer, int firstFinger, Vector2 first, Vector2 second, float start)
        {
            Sample(recognizer, firstFinger, TouchSamplePhase.Began, first, start, 1);
            Sample(recognizer, firstFinger + 1, TouchSamplePhase.Began, second, start + 0.03f, 2);
            Sample(recognizer, firstFinger, TouchSamplePhase.Ended, first, start + 0.12f, 2);
            Sample(recognizer, firstFinger + 1, TouchSamplePhase.Ended, second, start + 0.14f, 1);
        }

        private static void Touch(GestureRecognizer recognizer, int finger, Vector2 position, float down, float up)
        {
            Sample(recognizer, finger, TouchSamplePhase.Began, position, down, 1);
            Sample(recognizer, finger, TouchSamplePhase.Ended, position, up, 1);
        }

        private static void Sample(
            GestureRecognizer recognizer, int finger, TouchSamplePhase phase, Vector2 position, float time, int fingers)
        {
            recognizer.Process(new TouchSample(finger, phase, position, time, fingers));
        }

        private static GestureCommand For(GestureKind kind, Vector2 direction)
        {
            return GestureCommands.For(new Gesture(kind, Vector2.zero, 0f, direction.normalized), HalfAngle);
        }

        private sealed class CommandRecorder : IGestureSink
        {
            public readonly List<GestureCommand> Commands = new();

            public void OnGesture(in Gesture gesture)
            {
                var command = GestureCommands.For(gesture, HalfAngle);
                if (command != GestureCommand.None)
                {
                    Commands.Add(command);
                }
            }
        }
    }
}
