namespace DarknessRises.Game.Gestures
{
    /// <summary>
    /// A Hold: one finger, alone on the screen, kept still for the hold time. It starts then and ends when the finger
    /// lifts. Once started, the finger may move (to aim) and other fingers may join without ending it.
    /// </summary>
    public sealed class HoldRecognizer : IGestureRecognizer
    {
        private readonly GestureThresholds thresholds;
        private readonly LoneFingerPress press = new();
        private bool isHolding;

        public HoldRecognizer(GestureThresholds thresholds)
        {
            this.thresholds = thresholds;
        }

        public void Process(in TouchSample sample, IGestureSink sink)
        {
            var step = press.Track(sample);
            if (step == LoneFingerPress.Step.None)
            {
                return;
            }

            if (step == LoneFingerPress.Step.Lifted || step == LoneFingerPress.Step.Canceled)
            {
                if (isHolding)
                {
                    isHolding = false;
                    sink.OnGesture(new Gesture(GestureKind.HoldEnded, press.Current, press.EndTime));
                }

                return;
            }

            TryStart(sample.Time, sink);
        }

        public void Tick(float time, IGestureSink sink)
        {
            if (press.IsDown)
            {
                TryStart(time, sink);
            }
        }

        public void Reset()
        {
            press.Reset();
            isHolding = false;
        }

        private void TryStart(float time, IGestureSink sink)
        {
            if (isHolding || press.WasJoined || press.MaxMovement > thresholds.TapMaxMovement)
            {
                return;
            }

            if (time - press.StartTime >= thresholds.HoldSeconds)
            {
                isHolding = true;
                sink.OnGesture(new Gesture(GestureKind.HoldStarted, press.Current, time));
            }
        }
    }
}
