namespace DarknessRises.Game.Gestures
{
    /// <summary>
    /// A Tap: one finger, alone on the screen, down briefly and barely moved. Reported when it lifts.
    /// A touch that stayed long enough to start a Hold is the Hold's and never also a Tap, however the two times are
    /// authored, so one touch never fires twice.
    /// </summary>
    public sealed class TapRecognizer : IGestureRecognizer
    {
        private readonly GestureThresholds thresholds;
        private readonly LoneFingerPress press = new();
        private bool becameHold;

        public TapRecognizer(GestureThresholds thresholds)
        {
            this.thresholds = thresholds;
        }

        public void Process(in TouchSample sample, IGestureSink sink)
        {
            var step = press.Track(sample);
            if (step == LoneFingerPress.Step.Began)
            {
                becameHold = false;
            }

            if (step == LoneFingerPress.Step.Began || step == LoneFingerPress.Step.Moved)
            {
                NoteHold(sample.Time);
                return;
            }

            if (step == LoneFingerPress.Step.Lifted && !becameHold && press.WasTap(thresholds))
            {
                sink.OnGesture(new Gesture(GestureKind.Tap, press.Current, press.EndTime));
            }
        }

        public void Tick(float time, IGestureSink sink)
        {
            if (press.IsDown)
            {
                NoteHold(time);
            }
        }

        public void Reset()
        {
            press.Reset();
            becameHold = false;
        }

        // The same test, on the same samples and ticks, by which the Hold recognizer starts a Hold.
        private void NoteHold(float time)
        {
            if (!becameHold && !press.WasJoined && press.MaxMovement <= thresholds.TapMaxMovement &&
                time - press.StartTime >= thresholds.HoldSeconds)
            {
                becameHold = true;
            }
        }
    }
}
