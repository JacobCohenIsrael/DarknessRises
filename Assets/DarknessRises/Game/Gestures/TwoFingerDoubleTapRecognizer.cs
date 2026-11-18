using UnityEngine;

namespace DarknessRises.Game.Gestures
{
    /// <summary>
    /// A Two-finger double-tap: a Two-finger tap, then a second one whose first finger touches down soon after the
    /// first tap's last finger lifted, with the two taps' centroids close together. Reported when the second lifts.
    /// A third Two-finger tap starts over, so three quick ones make one Two-finger double-tap.
    /// Each of the two is still a Two-finger tap for the <see cref="TwoFingerTapRecognizer"/>.
    /// </summary>
    /// <remarks>
    /// Neither tap is ever a one-finger Tap: the second finger joins the first while it's down, so their
    /// <see cref="LoneFingerPress"/> marks it joined, or both land in the same frame and neither starts one.
    /// </remarks>
    public sealed class TwoFingerDoubleTapRecognizer : IGestureRecognizer
    {
        private readonly GestureThresholds thresholds;
        private readonly TwoFingerTapPress press = new();

        private bool hasFirstTap;
        private Vector2 firstTapCentroid;
        private float firstTapEndTime;

        public TwoFingerDoubleTapRecognizer(GestureThresholds thresholds)
        {
            this.thresholds = thresholds;
        }

        public void Process(in TouchSample sample, IGestureSink sink)
        {
            var outcome = press.Track(sample, thresholds);
            if (outcome == TwoFingerTapPress.Outcome.Pending)
            {
                return;
            }

            if (outcome == TwoFingerTapPress.Outcome.Missed)
            {
                // Anything else in between (a Tap, a Two-finger flick) breaks the pair.
                hasFirstTap = false;
                return;
            }

            var isSecond = hasFirstTap
                && press.StartTime - firstTapEndTime <= thresholds.DoubleTapMaxGapSeconds
                && (press.Centroid - firstTapCentroid).magnitude <= thresholds.DoubleTapMaxDistance;
            if (isSecond)
            {
                hasFirstTap = false;
                sink.OnGesture(new Gesture(GestureKind.TwoFingerDoubleTap, press.Centroid, press.EndTime));
                return;
            }

            hasFirstTap = true;
            firstTapCentroid = press.Centroid;
            firstTapEndTime = press.EndTime;
        }

        public void Tick(float time, IGestureSink sink)
        {
        }

        public void Reset()
        {
            press.Reset();
            hasFirstTap = false;
        }
    }
}
