using System.Collections.Generic;
using System.Linq;
using DarknessRises.Game.Gestures;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Gestures
{
    /// <summary>
    /// The Hold authored shorter than a Tap may last (as the game has it since #72), so a finger pressed on the screen
    /// shoots almost at once: one touch must still fire exactly once, and two-finger Gestures must still fire nothing.
    /// </summary>
    public sealed class QuickHoldGestureTests
    {
        // Tap ≤ 0.25 s and ≤ 0.03 moved, Hold after 0.15 s, Two-finger tap second finger within 0.15 s.
        private static readonly GestureThresholds Thresholds =
            new(0.25f, 0.03f, 0.15f, 0.3f, 0.1f, 0.08f, 0.3f, 0.15f, 0.08f, 0.35f, 35f);

        private static readonly Vector2 Middle = new(0.5f, 0.5f);
        private static readonly Vector2 Left = new(0.35f, 0.5f);
        private static readonly Vector2 Right = new(0.65f, 0.5f);

        private Recorded recorded;
        private GestureRecognizer recognizer;

        [SetUp]
        public void SetUp()
        {
            recorded = new Recorded();
            recognizer = GestureRecognizer.WithCoreGestures(Thresholds, recorded);
        }

        [Test]
        public void A_touch_shorter_than_the_Hold_is_a_Tap()
        {
            Sample(0, TouchSamplePhase.Began, Middle, 0f);
            recognizer.Tick(0.05f);
            Sample(0, TouchSamplePhase.Ended, Middle, 0.1f);

            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.Tap }));
        }

        [Test]
        public void A_still_touch_starts_a_Hold_after_the_short_hold_time()
        {
            Sample(0, TouchSamplePhase.Began, Middle, 0f);
            recognizer.Tick(0.1f);
            Assert.That(recorded.Kinds, Is.Empty);

            recognizer.Tick(0.16f);
            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.HoldStarted }));
        }

        [Test]
        public void A_touch_that_became_a_Hold_is_never_also_a_Tap()
        {
            // Down for 0.2 s: short enough to be a Tap by its own time, but the Hold already fired for it.
            Sample(0, TouchSamplePhase.Began, Middle, 0f);
            recognizer.Tick(0.16f);
            Sample(0, TouchSamplePhase.Ended, Middle, 0.2f);

            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.HoldStarted, GestureKind.HoldEnded }));
        }

        [Test]
        public void A_touch_that_lifts_before_any_tick_could_start_the_Hold_is_still_a_Tap()
        {
            // On a slow frame the lift arrives before the tick that would have started the Hold: the touch isn't lost.
            Sample(0, TouchSamplePhase.Began, Middle, 0f);
            recognizer.Tick(0.1f);
            Sample(0, TouchSamplePhase.Ended, Middle, 0.2f);
            recognizer.Tick(0.2f);

            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.Tap }));
        }

        [Test]
        public void A_second_finger_inside_the_two_finger_window_never_leaks_a_Hold()
        {
            // The second finger lands at the very end of the window, just as the Hold would have started.
            Sample(0, TouchSamplePhase.Began, Left, 0f, fingers: 1);
            recognizer.Tick(0.14f);
            Sample(1, TouchSamplePhase.Began, Right, 0.149f, fingers: 2);
            recognizer.Tick(0.2f);
            Sample(0, TouchSamplePhase.Ended, Left, 0.22f, fingers: 2);
            Sample(1, TouchSamplePhase.Ended, Right, 0.23f, fingers: 1);

            Assert.That(recorded.Kinds, Has.No.Member(GestureKind.HoldStarted));
            Assert.That(recorded.Kinds, Has.No.Member(GestureKind.Tap));
        }

        [Test]
        public void A_Flick_is_no_Hold_and_no_Tap()
        {
            Sample(0, TouchSamplePhase.Began, Middle, 0f);
            Sample(0, TouchSamplePhase.Moved, Middle + new Vector2(0.06f, 0f), 0.05f);
            recognizer.Tick(0.16f);
            Sample(0, TouchSamplePhase.Ended, Middle + new Vector2(0.15f, 0f), 0.18f);

            Assert.That(recorded.Kinds, Is.EqualTo(new[] { GestureKind.Flick }));
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
    }
}
