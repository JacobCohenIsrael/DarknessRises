using UnityEngine;

namespace DarknessRises.Game.Gestures
{
    /// <summary>
    /// Follows one attempt at a Two-finger tap: a second finger touches down within the Two-finger tap window after the
    /// first, while it's still down; neither stays down longer than a Tap nor moves more than one; no third finger joins.
    /// The building block of the Two-finger tap and the Two-finger double-tap; each recognizer owns its own.
    /// </summary>
    public sealed class TwoFingerTapPress
    {
        public enum Outcome
        {
            /// <summary>No attempt ended with this sample.</summary>
            Pending,

            /// <summary>Both fingers lifted: a Two-finger tap. Its numbers stay readable until the next one.</summary>
            Tapped,

            /// <summary>Every finger of an attempt lifted, but it wasn't a Two-finger tap (one finger, too slow, moved...).</summary>
            Missed,
        }

        private const int Fingers = 2;

        private readonly int[] fingerIds = new int[Fingers];
        private readonly Vector2[] starts = new Vector2[Fingers];
        private readonly float[] startTimes = new float[Fingers];
        private readonly bool[] lifted = new bool[Fingers];

        // How many fingers this attempt has seen touch down, and whether it already failed.
        private int count;
        private bool failed;

        /// <summary>The midpoint of where the two fingers touched down. Readable after a <see cref="Outcome.Tapped"/>.</summary>
        public Vector2 Centroid { get; private set; }

        /// <summary>When the first finger touched down. Readable after a <see cref="Outcome.Tapped"/>.</summary>
        public float StartTime { get; private set; }

        /// <summary>When the last finger lifted. Readable after a <see cref="Outcome.Tapped"/>.</summary>
        public float EndTime { get; private set; }

        /// <summary>Follows the sample, and says whether it ended an attempt and how.</summary>
        public Outcome Track(in TouchSample sample, GestureThresholds thresholds)
        {
            if (sample.Phase == TouchSamplePhase.Began)
            {
                Begin(sample, thresholds);
                return Outcome.Pending;
            }

            var index = IndexOf(sample.FingerId);
            if (index < 0)
            {
                return Outcome.Pending;
            }

            if ((sample.Position - starts[index]).magnitude > thresholds.TapMaxMovement
                || sample.Time - startTimes[index] > thresholds.TapMaxSeconds)
            {
                failed = true;
            }

            if (!sample.IsLift)
            {
                return Outcome.Pending;
            }

            if (sample.Phase == TouchSamplePhase.Canceled)
            {
                failed = true;
            }

            lifted[index] = true;
            if (!AllLifted())
            {
                return Outcome.Pending;
            }

            var recognized = count == Fingers && !failed;
            if (recognized)
            {
                Centroid = (starts[0] + starts[1]) * 0.5f;
                StartTime = startTimes[0];
                EndTime = sample.Time;
            }

            count = 0;
            failed = false;
            return recognized ? Outcome.Tapped : Outcome.Missed;
        }

        public void Reset()
        {
            count = 0;
            failed = false;
        }

        private void Begin(in TouchSample sample, GestureThresholds thresholds)
        {
            if (count == 0)
            {
                // A new attempt starts only on the screen's first (or first two, landing together) fingers.
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
