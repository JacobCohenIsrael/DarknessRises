namespace DarknessRises.Game.Gestures
{
    /// <summary>Where recognizers report the Gestures they recognize.</summary>
    public interface IGestureSink
    {
        void OnGesture(in Gesture gesture);
    }

    /// <summary>
    /// Recognizes one kind of Gesture from the stream of touch samples. Each recognizer sees every sample and keeps
    /// its own state, so a new Gesture (a circle, a line) is a new recognizer and never changes the existing ones.
    /// </summary>
    public interface IGestureRecognizer
    {
        void Process(in TouchSample sample, IGestureSink sink);

        /// <summary>Time passed with no new sample, for Gestures that happen by waiting (a hold).</summary>
        void Tick(float time, IGestureSink sink);

        /// <summary>Forgets any Gesture in progress.</summary>
        void Reset();
    }
}
