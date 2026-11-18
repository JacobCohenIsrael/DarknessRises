namespace DarknessRises.Game.Gestures
{
    /// <summary>A Tap: one finger, alone on the screen, down briefly and barely moved. Reported when it lifts.</summary>
    public sealed class TapRecognizer : IGestureRecognizer
    {
        private readonly GestureThresholds thresholds;
        private readonly LoneFingerPress press = new();

        public TapRecognizer(GestureThresholds thresholds)
        {
            this.thresholds = thresholds;
        }

        public void Process(in TouchSample sample, IGestureSink sink)
        {
            if (press.Track(sample) == LoneFingerPress.Step.Lifted && press.WasTap(thresholds))
            {
                sink.OnGesture(new Gesture(GestureKind.Tap, press.Current, press.EndTime));
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
