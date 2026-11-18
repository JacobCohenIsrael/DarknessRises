using UnityEngine;

namespace DarknessRises.Game.Gestures
{
    /// <summary>
    /// A Two-finger tap: a second finger touches down within the window after the first, while it's still down;
    /// neither stays down longer than a Tap nor moves more than one; no third finger joins. Reported when the last lifts.
    /// </summary>
    public sealed class TwoFingerTapRecognizer : IGestureRecognizer
    {
        private const int Fingers = 2;

        private readonly GestureThresholds thresholds;
        private readonly int[] fingerIds = new int[Fingers];
        private readonly Vector2[] starts = new Vector2[Fingers];
        private readonly float[] startTimes = new float[Fingers];
        private readonly bool[] lifted = new bool[Fingers];

        // How many fingers this attempt has seen touch down, and whether it already failed.
        private int count;
        private bool failed;

        public TwoFingerTapRecognizer(GestureThresholds thresholds)
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

            if ((sample.Position - starts[index]).magnitude > thresholds.TapMaxMovement
                || sample.Time - startTimes[index] > thresholds.TapMaxSeconds)
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

            var recognized = count == Fingers && !failed;
            var midpoint = (starts[0] + starts[1]) * 0.5f;
            count = 0;
            failed = false;
            if (recognized)
            {
                sink.OnGesture(new Gesture(GestureKind.TwoFingerTap, midpoint, sample.Time));
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

        private void Begin(in TouchSample sample)
        {
            if (count == 0)
            {
                // A new attempt starts only on the screen's first finger.
                if (sample.FingerCount > Fingers)
                {
                    return;
                }

                failed = false;
                Add(sample);
                return;
            }

            if (count >= Fingers || AnyLifted() || sample.Time - startTimes[0] > thresholds.TwoFingerTapWindowSeconds)
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
            startTimes[count] = sample.Time;
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
