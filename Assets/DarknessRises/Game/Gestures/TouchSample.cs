using UnityEngine;

namespace DarknessRises.Game.Gestures
{
    /// <summary>Where a finger is in its touch.</summary>
    public enum TouchSamplePhase
    {
        Began,
        Moved,
        Stationary,
        Ended,
        Canceled,
    }

    /// <summary>
    /// One finger's state at one moment, as the Gesture recognizer reads it.
    /// </summary>
    /// <remarks>
    /// <see cref="Position"/> is in screen units: pixels divided by the screen's shorter side, so thresholds are the
    /// same on every screen size. <see cref="FingerCount"/> counts every finger on the screen at that moment,
    /// including one lifting now.
    /// </remarks>
    public readonly struct TouchSample
    {
        public TouchSample(int fingerId, TouchSamplePhase phase, Vector2 position, float time, int fingerCount)
        {
            FingerId = fingerId;
            Phase = phase;
            Position = position;
            Time = time;
            FingerCount = fingerCount;
        }

        public int FingerId { get; }
        public TouchSamplePhase Phase { get; }
        public Vector2 Position { get; }

        /// <summary>Seconds, on a clock that keeps running while the game is paused.</summary>
        public float Time { get; }

        public int FingerCount { get; }

        public bool IsLift => Phase == TouchSamplePhase.Ended || Phase == TouchSamplePhase.Canceled;
    }
}
