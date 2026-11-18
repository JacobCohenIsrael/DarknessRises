namespace DarknessRises.Game.Gestures
{
    /// <summary>
    /// A Two-finger tap: a second finger touches down within the window after the first, while it's still down;
    /// neither stays down longer than a Tap nor moves more than one; no third finger joins. Reported when the last lifts,
    /// at the midpoint of where the fingers touched down.
    /// </summary>
    public sealed class TwoFingerTapRecognizer : IGestureRecognizer
    {
        private readonly GestureThresholds thresholds;
        private readonly TwoFingerTapPress press = new();

        public TwoFingerTapRecognizer(GestureThresholds thresholds)
        {
            this.thresholds = thresholds;
        }

        public void Process(in TouchSample sample, IGestureSink sink)
        {
            if (press.Track(sample, thresholds) == TwoFingerTapPress.Outcome.Tapped)
            {
                sink.OnGesture(new Gesture(GestureKind.TwoFingerTap, press.Centroid, press.EndTime));
            }
        }

        public void Tick(float time, IGestureSink sink)
        {
        }

        public void Reset()
        {
            press.Reset();
        }
    }
}
