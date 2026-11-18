using UnityEngine;

namespace DarknessRises.Game.Gestures
{
    public enum GestureKind
    {
        /// <summary>One finger, briefly down and barely moved.</summary>
        Tap,

        /// <summary>One finger kept down and still long enough to count as held.</summary>
        HoldStarted,

        /// <summary>The held finger lifted.</summary>
        HoldEnded,

        /// <summary>A second Tap soon after and close to the first.</summary>
        DoubleTap,

        /// <summary>One finger moved far and fast, then lifted. Has a direction.</summary>
        Flick,

        /// <summary>Two fingers down together, briefly, barely moved.</summary>
        TwoFingerTap,
    }

    /// <summary>A recognized Gesture: what it was, where, and for a Flick which way (screen space, x right, y up).</summary>
    public readonly struct Gesture
    {
        public Gesture(GestureKind kind, Vector2 position, float time, Vector2 direction = default)
        {
            Kind = kind;
            Position = position;
            Time = time;
            Direction = direction;
        }

        public GestureKind Kind { get; }
        public Vector2 Position { get; }
        public float Time { get; }

        /// <summary>A Flick's unit direction. Zero for the other Gestures.</summary>
        public Vector2 Direction { get; }
    }
}
