using System.Collections.Generic;
using System.Linq;
using DarknessRises.Game.Gestures;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Gestures
{
    public sealed class GestureRecognizerTests
    {
        // Tap ≤ 0.25 s and ≤ 0.03 moved, Hold after 0.3 s, Two-finger double-tap gap ≤ 0.3 s and centroids ≤ 0.1 apart,
        // Flick ≥ 0.08 in ≤ 0.3 s, Two-finger tap second finger within 0.15 s,
        // Two-finger flick each finger ≥ 0.08 in ≤ 0.35 s, at most 35° apart.
        private static readonly GestureThresholds Thresholds =
            new(0.25f, 0.03f, 0.3f, 0.3f, 0.1f, 0.08f, 0.3f, 0.15f, 0.08f, 0.35f, 35f);

        private static readonly Vector2 Left = new(0.35f, 0.5f);
        private static readonly Vector2 Right = new(0.65f, 0.5f);

        private static readonly Vector2 Middle = new(0.5f, 0.5f);

        private Recorded recorded;
        private GestureRecognizer recognizer;
        private int nextFingerId;

        [SetUp]
        public void SetUp()
        {
            recorded = new Recorded();
            recognizer = GestureRecognizer.WithCoreGestures(Thresholds, recorded);
            nextFingerId = 100;
        }

        [Test]
        public void ABriefStillTouchIsATap()
        {
            Touch(0, Middle, 0f, 0.1f);

            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.Tap }));
        }

        [Test]
        public void ATouchLongerThanATapIsNoTap()
        {
            Touch(0, Middle, 0f, 0.28f);

            Assert.That(recorded.Kinds, Is.Empty);
        }

        [Test]
        public void ATouchThatMovesIsNoTap()
        {
            Down(0, Middle, 0f);
            Lift(0, Middle + new Vector2(0.05f, 0f), 0.1f);

            Assert.That(recorded.Kinds, Has.No.Member(GestureKind.Tap));
        }

        [Test]
        public void AStillFingerStartsAHoldOnceTheHoldTimePassesAndEndsItWhenLifted()
        {
            Down(0, Middle, 0f);
            recognizer.Tick(0.2f);
            Assert.That(recorded.Kinds, Is.Empty);

            recognizer.Tick(0.31f);
            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.HoldStarted }));

            // Once held, the finger may move to aim without ending the Hold.
            Sample(0, TouchSamplePhase.Moved, Middle + new Vector2(0.2f, 0f), 0.5f);
            Lift(0, Middle + new Vector2(0.2f, 0f), 0.9f);

            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.HoldStarted, GestureKind.HoldEnded }));
        }

        [Test]
        public void AFingerThatMovesBeforeTheHoldTimeIsNoHold()
        {
            Down(0, Middle, 0f);
            Sample(0, TouchSamplePhase.Moved, Middle + new Vector2(0.05f, 0f), 0.1f);
            recognizer.Tick(0.4f);

            Assert.That(recorded.Kinds, Has.No.Member(GestureKind.HoldStarted));
        }

        [Test]
        public void TwoQuickOneFingerTapsCloseTogetherAreJustTwoTaps()
        {
            Touch(0, Middle, 0f, 0.1f);
            Touch(1, Middle + new Vector2(0.02f, 0f), 0.3f, 0.38f);

            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.Tap, GestureKind.Tap }));
        }

        [Test]
        public void TwoQuickOneFingerTapsOnTwoTargetsAreJustTwoTaps()
        {
            Touch(0, Left, 0f, 0.08f);
            Touch(1, Right, 0.15f, 0.22f);

            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.Tap, GestureKind.Tap }));
        }

        [Test]
        public void TwoQuickTwoFingerTapsCloseTogetherAreATwoFingerDoubleTapAndNoTap()
        {
            TwoFingerTap(Left, Right, 0f);
            TwoFingerTap(Left + new Vector2(0.02f, 0.01f), Right + new Vector2(0.03f, -0.01f), 0.3f);

            Assert.That(recorded.Kinds, Is.EqualTo(new[]
            {
                GestureKind.TwoFingerTap, GestureKind.TwoFingerTap, GestureKind.TwoFingerDoubleTap,
            }));
            Assert.That(recorded.Gestures[2].Position.x, Is.EqualTo(0.525f).Within(1e-5f));
        }

        [Test]
        public void TwoFingerTapsLandingInTheSameFrameMakeATwoFingerDoubleTap()
        {
            for (var tap = 0; tap < 2; tap++)
            {
                var finger = tap * 2;
                var time = tap * 0.25f;
                Sample(finger, TouchSamplePhase.Began, Left, time, fingers: 2);
                Sample(finger + 1, TouchSamplePhase.Began, Right, time, fingers: 2);
                Sample(finger, TouchSamplePhase.Ended, Left, time + 0.08f, fingers: 2);
                Sample(finger + 1, TouchSamplePhase.Ended, Right, time + 0.08f, fingers: 2);
            }

            Assert.That(recorded.Kinds, Has.No.Member(GestureKind.Tap));
            Assert.That(recorded.Kinds, Has.Member(GestureKind.TwoFingerDoubleTap));
        }

        [Test]
        public void OneTwoFingerTapIsNoTwoFingerDoubleTap()
        {
            TwoFingerTap(Left, Right, 0f);

            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.TwoFingerTap }));
        }

        [Test]
        public void ASecondTwoFingerTapTooLateIsNoTwoFingerDoubleTap()
        {
            // The first tap lifts at 0.14 s; the second lands 0.36 s later, past the 0.3 s gap.
            TwoFingerTap(Left, Right, 0f);
            TwoFingerTap(Left, Right, 0.5f);

            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.TwoFingerTap, GestureKind.TwoFingerTap }));
        }

        [Test]
        public void ASecondTwoFingerTapTooFarAwayIsNoTwoFingerDoubleTap()
        {
            var away = new Vector2(0f, 0.2f);
            TwoFingerTap(Left, Right, 0f);
            TwoFingerTap(Left + away, Right + away, 0.25f);

            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.TwoFingerTap, GestureKind.TwoFingerTap }));
        }

        [Test]
        public void AOneFingerTapBetweenTwoFingerTapsBreaksTheTwoFingerDoubleTap()
        {
            TwoFingerTap(Left, Right, 0f);
            Touch(9, Middle, 0.16f, 0.2f);
            TwoFingerTap(Left, Right, 0.25f);

            Assert.That(recorded.Kinds, Has.No.Member(GestureKind.TwoFingerDoubleTap));
        }

        [Test]
        public void ThreeQuickTwoFingerTapsAreOneTwoFingerDoubleTap()
        {
            TwoFingerTap(Left, Right, 0f);
            TwoFingerTap(Left, Right, 0.25f);
            TwoFingerTap(Left, Right, 0.5f);

            Assert.That(recorded.Kinds.Count(kind => kind == GestureKind.TwoFingerDoubleTap), Is.EqualTo(1));
        }

        [Test]
        public void TwoQuickTwoFingerFlicksAreNoTwoFingerDoubleTap()
        {
            TwoFingerFlick(new Vector2(0f, 0.2f), new Vector2(0f, 0.2f), 0.12f, startTime: 0f, firstFinger: 0);
            TwoFingerFlick(new Vector2(0f, 0.2f), new Vector2(0f, 0.2f), 0.12f, startTime: 0.2f, firstFinger: 2);

            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.TwoFingerFlick, GestureKind.TwoFingerFlick }));
        }

        [Test]
        public void AFastLongMoveIsAFlickInItsDirection()
        {
            Down(0, Middle, 0f);
            Sample(0, TouchSamplePhase.Moved, Middle + new Vector2(0f, -0.1f), 0.08f);
            Lift(0, Middle + new Vector2(0f, -0.2f), 0.15f);

            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.Flick }));
            Assert.That(recorded.Gestures[0].Direction.x, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(recorded.Gestures[0].Direction.y, Is.EqualTo(-1f).Within(1e-5f));
        }

        [Test]
        public void ASlowMoveIsNoFlick()
        {
            Down(0, Middle, 0f);
            Sample(0, TouchSamplePhase.Moved, Middle + new Vector2(0.1f, 0f), 0.4f);
            Lift(0, Middle + new Vector2(0.2f, 0f), 0.8f);

            Assert.That(recorded.Kinds, Has.No.Member(GestureKind.Flick));
        }

        [Test]
        public void AShortMoveIsNoFlick()
        {
            Down(0, Middle, 0f);
            Lift(0, Middle + new Vector2(0.05f, 0f), 0.1f);

            Assert.That(recorded.Kinds, Is.Empty);
        }

        [Test]
        public void TwoFingersTappedTogetherAreATwoFingerTapAndNotTwoTaps()
        {
            Sample(0, TouchSamplePhase.Began, Middle, 0f, fingers: 1);
            Sample(1, TouchSamplePhase.Began, Middle + new Vector2(0.2f, 0f), 0.05f, fingers: 2);
            Sample(0, TouchSamplePhase.Ended, Middle, 0.12f, fingers: 2);
            Sample(1, TouchSamplePhase.Ended, Middle + new Vector2(0.2f, 0f), 0.14f, fingers: 1);

            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.TwoFingerTap }));
        }

        [Test]
        public void TwoFingersLandingInTheSameFrameAreATwoFingerTap()
        {
            Sample(0, TouchSamplePhase.Began, Middle, 0f, fingers: 2);
            Sample(1, TouchSamplePhase.Began, Middle + new Vector2(0.2f, 0f), 0f, fingers: 2);
            Sample(0, TouchSamplePhase.Ended, Middle, 0.1f, fingers: 2);
            Sample(1, TouchSamplePhase.Ended, Middle + new Vector2(0.2f, 0f), 0.1f, fingers: 2);

            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.TwoFingerTap }));
        }

        [Test]
        public void ASecondFingerTooLateIsNoTwoFingerTap()
        {
            Sample(0, TouchSamplePhase.Began, Middle, 0f, fingers: 1);
            Sample(1, TouchSamplePhase.Began, Middle + new Vector2(0.2f, 0f), 0.2f, fingers: 2);
            Sample(0, TouchSamplePhase.Ended, Middle, 0.22f, fingers: 2);
            Sample(1, TouchSamplePhase.Ended, Middle + new Vector2(0.2f, 0f), 0.24f, fingers: 1);

            Assert.That(recorded.Kinds, Is.Empty);
        }

        [Test]
        public void TwoFingersHeldDownTooLongAreNoTwoFingerTap()
        {
            Sample(0, TouchSamplePhase.Began, Middle, 0f, fingers: 1);
            Sample(1, TouchSamplePhase.Began, Middle + new Vector2(0.2f, 0f), 0.05f, fingers: 2);
            Sample(0, TouchSamplePhase.Ended, Middle, 0.6f, fingers: 2);
            Sample(1, TouchSamplePhase.Ended, Middle + new Vector2(0.2f, 0f), 0.6f, fingers: 1);

            Assert.That(recorded.Kinds, Is.Empty);
        }

        [Test]
        public void ThreeFingersAreNoTwoFingerTap()
        {
            Sample(0, TouchSamplePhase.Began, Middle, 0f, fingers: 1);
            Sample(1, TouchSamplePhase.Began, Middle + new Vector2(0.1f, 0f), 0.02f, fingers: 2);
            Sample(2, TouchSamplePhase.Began, Middle + new Vector2(0.2f, 0f), 0.04f, fingers: 3);
            Sample(0, TouchSamplePhase.Ended, Middle, 0.1f, fingers: 3);
            Sample(1, TouchSamplePhase.Ended, Middle + new Vector2(0.1f, 0f), 0.1f, fingers: 2);
            Sample(2, TouchSamplePhase.Ended, Middle + new Vector2(0.2f, 0f), 0.1f, fingers: 1);

            Assert.That(recorded.Kinds, Is.Empty);
        }

        [Test]
        public void TwoFingersFlickedUpTogetherAreATwoFingerFlickUpAndNothingElse()
        {
            TwoFingerFlick(new Vector2(0f, 0.2f), new Vector2(0.03f, 0.19f), 0.15f);

            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.TwoFingerFlick }));
            Assert.That(recorded.Gestures[0].Direction.y, Is.GreaterThan(0.99f));
            Assert.That(recorded.Gestures[0].Position, Is.EqualTo((Left + Right) * 0.5f));
        }

        [Test]
        public void TwoFingersFlickedDownTogetherAreATwoFingerFlickDownAndNothingElse()
        {
            TwoFingerFlick(new Vector2(0f, -0.2f), new Vector2(-0.02f, -0.18f), 0.15f);

            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.TwoFingerFlick }));
            Assert.That(recorded.Gestures[0].Direction.y, Is.LessThan(-0.99f));
        }

        [Test]
        public void TwoFingersLandingInTheSameFrameCanFlick()
        {
            Sample(0, TouchSamplePhase.Began, Left, 0f, fingers: 2);
            Sample(1, TouchSamplePhase.Began, Right, 0f, fingers: 2);
            Sample(0, TouchSamplePhase.Ended, Left + new Vector2(0f, 0.2f), 0.12f, fingers: 2);
            Sample(1, TouchSamplePhase.Ended, Right + new Vector2(0f, 0.2f), 0.12f, fingers: 1);

            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.TwoFingerFlick }));
        }

        [Test]
        public void ATooSlowTwoFingerFlickIsNothing()
        {
            TwoFingerFlick(new Vector2(0f, 0.2f), new Vector2(0f, 0.2f), 0.5f);

            Assert.That(recorded.Kinds, Is.Empty);
        }

        [Test]
        public void TwoFingersMovingTooLittleAreNoTwoFingerFlick()
        {
            TwoFingerFlick(new Vector2(0f, 0.2f), new Vector2(0f, 0.05f), 0.15f);

            Assert.That(recorded.Kinds, Is.Empty);
        }

        [Test]
        public void TwoFingersMovingApartAreNoTwoFingerFlick()
        {
            // Spread: the left finger goes up-left, the right one up-right, 90° apart.
            TwoFingerFlick(new Vector2(-0.15f, 0.15f), new Vector2(0.15f, 0.15f), 0.15f);

            Assert.That(recorded.Kinds, Is.Empty);
        }

        [Test]
        public void TwoFingersMovingOppositeWaysAreNoTwoFingerFlick()
        {
            TwoFingerFlick(new Vector2(0f, 0.2f), new Vector2(0f, -0.2f), 0.15f);

            Assert.That(recorded.Kinds, Is.Empty);
        }

        [Test]
        public void AThirdFingerCancelsATwoFingerFlick()
        {
            var up = new Vector2(0f, 0.2f);
            Sample(0, TouchSamplePhase.Began, Left, 0f, fingers: 1);
            Sample(1, TouchSamplePhase.Began, Right, 0.03f, fingers: 2);
            Sample(2, TouchSamplePhase.Began, Middle, 0.05f, fingers: 3);
            Sample(0, TouchSamplePhase.Ended, Left + up, 0.15f, fingers: 3);
            Sample(1, TouchSamplePhase.Ended, Right + up, 0.15f, fingers: 2);
            Sample(2, TouchSamplePhase.Ended, Middle + up, 0.15f, fingers: 1);

            Assert.That(recorded.Kinds, Is.Empty);
        }

        [Test]
        public void ASecondFingerTooLateIsNoTwoFingerFlick()
        {
            var up = new Vector2(0f, 0.2f);
            Sample(0, TouchSamplePhase.Began, Left, 0f, fingers: 1);
            Sample(1, TouchSamplePhase.Began, Right, 0.2f, fingers: 2);
            Sample(0, TouchSamplePhase.Ended, Left + up, 0.3f, fingers: 2);
            Sample(1, TouchSamplePhase.Ended, Right + up, 0.3f, fingers: 1);

            Assert.That(recorded.Kinds, Has.No.Member(GestureKind.TwoFingerFlick));
            Assert.That(recorded.Kinds, Has.No.Member(GestureKind.Flick));
        }

        [Test]
        public void AFirstFingerHeldStillBeforeTheSecondJoinsLeaksNoHold()
        {
            // The second finger joins just before the hold time, then both flick.
            var up = new Vector2(0f, 0.2f);
            Sample(0, TouchSamplePhase.Began, Left, 0f, fingers: 1);
            recognizer.Tick(0.1f);
            Sample(1, TouchSamplePhase.Began, Right, 0.12f, fingers: 2);
            recognizer.Tick(0.31f);
            Sample(0, TouchSamplePhase.Ended, Left + up, 0.32f, fingers: 2);
            Sample(1, TouchSamplePhase.Ended, Right + up, 0.32f, fingers: 1);

            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.TwoFingerFlick }));
        }

        [Test]
        public void ACanceledTouchIsNoGesture()
        {
            Down(0, Middle, 0f);
            Sample(0, TouchSamplePhase.Canceled, Middle, 0.1f);

            Assert.That(recorded.Kinds, Is.Empty);
        }

        [Test]
        public void ANewGestureRegistersWithoutChangingTheOthers()
        {
            var lifts = new LiftCounter();
            recognizer.Register(lifts);

            Touch(0, Middle, 0f, 0.1f);

            Assert.That(lifts.Count, Is.EqualTo(1));
            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.Tap }));
        }

        // Two fingers land 0.03 s apart at Left and Right, move halfway, then lift the given seconds after the first landed.
        private void TwoFingerFlick(
            Vector2 leftTravel, Vector2 rightTravel, float liftTime, float startTime = 0f, int firstFinger = 0)
        {
            var second = firstFinger + 1;
            Sample(firstFinger, TouchSamplePhase.Began, Left, startTime, fingers: 1);
            Sample(second, TouchSamplePhase.Began, Right, startTime + 0.03f, fingers: 2);
            Sample(firstFinger, TouchSamplePhase.Moved, Left + leftTravel * 0.5f, startTime + liftTime * 0.5f, fingers: 2);
            Sample(second, TouchSamplePhase.Moved, Right + rightTravel * 0.5f, startTime + liftTime * 0.5f, fingers: 2);
            Sample(firstFinger, TouchSamplePhase.Ended, Left + leftTravel, startTime + liftTime, fingers: 2);
            Sample(second, TouchSamplePhase.Ended, Right + rightTravel, startTime + liftTime, fingers: 1);
        }

        // Two fingers land 0.03 s apart at the given points and lift 0.12 s and 0.14 s after the first landed, unmoved.
        // Finger ids are new for every tap, as on a real screen.
        private void TwoFingerTap(Vector2 first, Vector2 second, float startTime)
        {
            var id = nextFingerId;
            nextFingerId += 2;
            Sample(id, TouchSamplePhase.Began, first, startTime, fingers: 1);
            Sample(id + 1, TouchSamplePhase.Began, second, startTime + 0.03f, fingers: 2);
            Sample(id, TouchSamplePhase.Ended, first, startTime + 0.12f, fingers: 2);
            Sample(id + 1, TouchSamplePhase.Ended, second, startTime + 0.14f, fingers: 1);
        }

        private void Touch(int finger, Vector2 position, float down, float up)
        {
            Down(finger, position, down);
            Lift(finger, position, up);
        }

        private void Down(int finger, Vector2 position, float time)
        {
            Sample(finger, TouchSamplePhase.Began, position, time);
        }

        private void Lift(int finger, Vector2 position, float time)
        {
            Sample(finger, TouchSamplePhase.Ended, position, time);
        }

        private void Sample(int finger, TouchSamplePhase phase, Vector2 position, float time, int fingers = 1)
        {
            recognizer.Process(new TouchSample(finger, phase, position, time, fingers));
        }

        private sealed class Recorded : IGestureSink
        {
            public readonly List<Gesture> Gestures = new();

            public List<GestureKind> Kinds => Gestures.Select(gesture => gesture.Kind).ToList();

            public void OnGesture(in Gesture gesture)
            {
                Gestures.Add(gesture);
            }
        }

        // A stand-in for a future Gesture: counts lifted fingers.
        private sealed class LiftCounter : IGestureRecognizer
        {
            public int Count { get; private set; }

            public void Process(in TouchSample sample, IGestureSink sink)
            {
                if (sample.IsLift)
                {
                    Count++;
                }
            }

            public void Tick(float time, IGestureSink sink)
            {
            }

            public void Reset()
            {
                Count = 0;
            }
        }
    }
}
