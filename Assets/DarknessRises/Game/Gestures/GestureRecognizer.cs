using System.Collections.Generic;

namespace DarknessRises.Game.Gestures
{
    /// <summary>
    /// Turns touch samples into Gestures by passing each sample to every registered recognizer.
    /// New Gestures are added with <see cref="Register"/>, without changing the existing recognizers.
    /// </summary>
    public sealed class GestureRecognizer
    {
        private readonly List<IGestureRecognizer> recognizers = new();
        private readonly IGestureSink sink;

        public GestureRecognizer(IGestureSink sink)
        {
            this.sink = sink;
        }

        /// <summary>The recognizers for the core Gestures: Tap, Hold, Double-tap, Flick and Two-finger tap.</summary>
        public static GestureRecognizer WithCoreGestures(GestureThresholds thresholds, IGestureSink sink)
        {
            var recognizer = new GestureRecognizer(sink);
            recognizer.Register(new TapRecognizer(thresholds));
            recognizer.Register(new HoldRecognizer(thresholds));
            recognizer.Register(new DoubleTapRecognizer(thresholds));
            recognizer.Register(new FlickRecognizer(thresholds));
            recognizer.Register(new TwoFingerTapRecognizer(thresholds));
            return recognizer;
        }

        public void Register(IGestureRecognizer recognizer)
        {
            recognizers.Add(recognizer);
        }

        public void Process(in TouchSample sample)
        {
            for (var i = 0; i < recognizers.Count; i++)
            {
                recognizers[i].Process(sample, sink);
            }
        }

        public void Tick(float time)
        {
            for (var i = 0; i < recognizers.Count; i++)
            {
                recognizers[i].Tick(time, sink);
            }
        }

        public void Reset()
        {
            for (var i = 0; i < recognizers.Count; i++)
            {
                recognizers[i].Reset();
            }
        }
    }
}
