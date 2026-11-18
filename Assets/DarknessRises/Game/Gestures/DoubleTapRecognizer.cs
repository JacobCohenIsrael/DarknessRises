namespace DarknessRises.Game.Gestures
{
    /// <summary>
    /// A Double-tap: a Tap, then a second Tap that touches down soon after the first lifted, close to it.
    /// Reported when the second lifts. A third Tap starts over, so three quick Taps make one Double-tap.
    /// Each of the two Taps is still a Tap for the <see cref="TapRecognizer"/>.
    /// </summary>
    public sealed class DoubleTapRecognizer : IGestureRecognizer
    {
        private readonly GestureThresholds thresholds;
        private readonly LoneFingerPress press = new();

        private bool hasFirstTap;
        private UnityEngine.Vector2 firstTapPosition;
        private float firstTapEndTime;

        public DoubleTapRecognizer(GestureThresholds thresholds)
        {
            this.thresholds = thresholds;
        }

        public void Process(in TouchSample sample, IGestureSink sink)
        {
            if (press.Track(sample) != LoneFingerPress.Step.Lifted)
            {
                return;
            }

            if (!press.WasTap(thresholds))
            {
                hasFirstTap = false;
                return;
            }

            var isSecond = hasFirstTap
                && press.StartTime - firstTapEndTime <= thresholds.DoubleTapMaxGapSeconds
                && (press.Start - firstTapPosition).magnitude <= thresholds.DoubleTapMaxDistance;
            if (isSecond)
            {
                hasFirstTap = false;
                sink.OnGesture(new Gesture(GestureKind.DoubleTap, press.Current, press.EndTime));
                return;
            }

            hasFirstTap = true;
            firstTapPosition = press.Start;
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
