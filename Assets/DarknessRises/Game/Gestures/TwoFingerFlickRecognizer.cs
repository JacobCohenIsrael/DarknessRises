using UnityEngine;

namespace DarknessRises.Game.Gestures
{
    /// <summary>
    /// A Two-finger flick: a second finger touches down within the Two-finger tap window after the first, while it's
    /// still down; both move at least the Two-finger flick distance, the same way (the angle between their movements is
    /// within the authored limit), and both lift within the Two-finger flick time; no third finger joins.
    /// Reported when the last lifts, with the fingers' average direction.
    /// </summary>
    /// <remarks>
    /// The one-finger Gestures never fire from either finger: their <see cref="LoneFingerPress"/> sees the second finger
    /// join, and the movement is too large for a Two-finger tap.
    /// </remarks>
    public sealed class TwoFingerFlickRecognizer : IGestureRecognizer
    {
        private const int Fingers = 2;

        private readonly GestureThresholds thresholds;
        private readonly int[] fingerIds = new int[Fingers];
        private readonly Vector2[] starts = new Vector2[Fingers];
        private readonly Vector2[] ends = new Vector2[Fingers];
        private readonly bool[] lifted = new bool[Fingers];

        // How many fingers this attempt has seen touch down, when the first did, and whether the attempt already failed.
        private int count;
        private float firstStartTime;
        private bool failed;

        public TwoFingerFlickRecognizer(GestureThresholds thresholds)
        {
            this.thresholds = thresholds;
        }

        public void Process(in TouchSample sample, IGestureSink sink)
        {
            if (sample.Phase == TouchSamplePhase.Began)
            {
                Begin(sample);
                return;
            }

            var index = IndexOf(sample.FingerId);
            if (index < 0)
            {
                return;
            }

            ends[index] = sample.Position;
            if (sample.Time - firstStartTime > thresholds.TwoFingerFlickMaxSeconds)
            {
                failed = true;
            }

            if (!sample.IsLift)
            {
                return;
            }

            if (sample.Phase == TouchSamplePhase.Canceled)
            {
                failed = true;
            }

            lifted[index] = true;
            if (!AllLifted())
            {
                return;
            }

            var direction = Vector2.zero;
            var recognized = count == Fingers && !failed && TryGetDirection(out direction);
            var midpoint = (starts[0] + starts[1]) * 0.5f;
            count = 0;
            failed = false;
            if (recognized)
            {
                sink.OnGesture(new Gesture(GestureKind.TwoFingerFlick, midpoint, sample.Time, direction));
            }
        }

        public void Tick(float time, IGestureSink sink)
        {
        }

        public void Reset()
        {
            count = 0;
            failed = false;
        }

        private bool TryGetDirection(out Vector2 direction)
        {
            direction = Vector2.zero;
            var first = ends[0] - starts[0];
            var second = ends[1] - starts[1];
            var minDistance = Mathf.Max(thresholds.TwoFingerFlickMinDistance, Mathf.Epsilon);
            if (first.magnitude < minDistance || second.magnitude < minDistance)
            {
                return false;
            }

            if (Vector2.Angle(first, second) > thresholds.TwoFingerFlickMaxAngleDegrees)
            {
                return false;
            }

            var sum = first.normalized + second.normalized;
            if (sum == Vector2.zero)
            {
                return false;
            }

            direction = sum.normalized;
            return true;
        }

        private void Begin(in TouchSample sample)
        {
            if (count == 0)
            {
                // A new attempt starts only on the screen's first (or first two, landing together) fingers.
                if (sample.FingerCount > Fingers)
                {
                    return;
                }

                failed = false;
                firstStartTime = sample.Time;
                Add(sample);
                return;
            }

            if (count >= Fingers || AnyLifted() || sample.Time - firstStartTime > thresholds.TwoFingerTapWindowSeconds)
            {
                failed = true;
                if (count < Fingers)
                {
                    Add(sample);
                }

                return;
            }

            Add(sample);
        }

        private void Add(in TouchSample sample)
        {
            fingerIds[count] = sample.FingerId;
            starts[count] = sample.Position;
            ends[count] = sample.Position;
            lifted[count] = false;
            count++;
        }

        private int IndexOf(int fingerId)
        {
            for (var i = 0; i < count; i++)
            {
                if (fingerIds[i] == fingerId && !lifted[i])
                {
                    return i;
                }
            }

            return -1;
        }

        private bool AllLifted()
        {
            for (var i = 0; i < count; i++)
            {
                if (!lifted[i])
                {
                    return false;
                }
            }

            return true;
        }

        private bool AnyLifted()
        {
            for (var i = 0; i < count; i++)
            {
                if (lifted[i])
                {
                    return true;
                }
            }

            return false;
        }
    }
}
