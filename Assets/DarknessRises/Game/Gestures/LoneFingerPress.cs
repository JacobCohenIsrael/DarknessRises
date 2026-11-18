using UnityEngine;

namespace DarknessRises.Game.Gestures
{
    /// <summary>
    /// Follows one finger that touched down while no other finger was on the screen, until it lifts.
    /// It remembers whether another finger joined meanwhile, how far it strayed from where it started, and for how long.
    /// The building block of the one-finger Gestures; each recognizer owns its own.
    /// </summary>
    public sealed class LoneFingerPress
    {
        public enum Step
        {
            /// <summary>The sample didn't concern the followed finger.</summary>
            None,
            Began,
            Moved,

            /// <summary>The finger lifted. Its numbers stay readable until the next press begins.</summary>
            Lifted,

            /// <summary>The touch was canceled by the system. Never a Gesture.</summary>
            Canceled,
        }

        private int fingerId;

        public bool IsDown { get; private set; }

        /// <summary>Whether another finger touched the screen while this one was down.</summary>
        public bool WasJoined { get; private set; }

        public Vector2 Start { get; private set; }
        public Vector2 Current { get; private set; }
        public float StartTime { get; private set; }
        public float EndTime { get; private set; }

        /// <summary>The farthest the finger got from where it touched down.</summary>
        public float MaxMovement { get; private set; }

        public float Seconds => EndTime - StartTime;

        public Step Track(in TouchSample sample)
        {
            if (!IsDown)
            {
                // Only a finger alone on the screen starts a press.
                if (sample.Phase != TouchSamplePhase.Began || sample.FingerCount > 1)
                {
                    return Step.None;
                }

                IsDown = true;
                WasJoined = false;
                fingerId = sample.FingerId;
                Start = sample.Position;
                Current = sample.Position;
                StartTime = sample.Time;
                EndTime = sample.Time;
                MaxMovement = 0f;
                return Step.Began;
            }

            if (sample.FingerCount > 1)
            {
                WasJoined = true;
            }

            if (sample.FingerId != fingerId)
            {
                return Step.None;
            }

            Current = sample.Position;
            EndTime = sample.Time;
            var movement = (Current - Start).magnitude;
            if (movement > MaxMovement)
            {
                MaxMovement = movement;
            }

            if (sample.IsLift)
            {
                IsDown = false;
                return sample.Phase == TouchSamplePhase.Canceled ? Step.Canceled : Step.Lifted;
            }

            return Step.Moved;
        }

        /// <summary>Whether the finger was a Tap: alone, brief and barely moved.</summary>
        public bool WasTap(GestureThresholds thresholds)
        {
            return !WasJoined && Seconds <= thresholds.TapMaxSeconds && MaxMovement <= thresholds.TapMaxMovement;
        }

        public void Reset()
        {
            IsDown = false;
            WasJoined = false;
            MaxMovement = 0f;
        }
    }
}
