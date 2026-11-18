namespace DarknessRises.Game.Gestures
{
    /// <summary>
    /// A Flick: one finger, alone on the screen, moved at least the Flick distance and lifted within the Flick time.
    /// Reported when it lifts, with the direction from where it touched down to where it lifted.
    /// </summary>
    public sealed class FlickRecognizer : IGestureRecognizer
    {
        private readonly GestureThresholds thresholds;
        private readonly LoneFingerPress press = new();

        public FlickRecognizer(GestureThresholds thresholds)
        {
            this.thresholds = thresholds;
        }

        public void Process(in TouchSample sample, IGestureSink sink)
        {
            if (press.Track(sample) != LoneFingerPress.Step.Lifted || press.WasJoined
                || press.Seconds > thresholds.FlickMaxSeconds)
            {
                return;
            }

            var travel = press.Current - press.Start;
            var distance = travel.magnitude;
            if (distance < thresholds.FlickMinDistance || distance <= 0f)
            {
                return;
            }

            sink.OnGesture(new Gesture(GestureKind.Flick, press.Start, press.EndTime, travel / distance));
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
