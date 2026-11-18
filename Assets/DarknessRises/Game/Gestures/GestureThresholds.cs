using System;
using UnityEngine;

namespace DarknessRises.Game.Gestures
{
    /// <summary>
    /// What counts as each Gesture. Distances are in screen units: fractions of the screen's shorter side.
    /// </summary>
    [Serializable]
    public sealed class GestureThresholds
    {
        [SerializeField, Min(0f), Tooltip("Seconds a Tap's finger may stay down at most.")]
        private float tapMaxSeconds = 0.25f;

        [SerializeField, Min(0f), Tooltip("How far a Tap's (or a Hold's, before it starts) finger may move, in screen units.")]
        private float tapMaxMovement = 0.03f;

        [SerializeField, Min(0f), Tooltip("Seconds a still finger must stay down to start a Hold. Longer than a Tap.")]
        private float holdSeconds = 0.3f;

        [SerializeField, Min(0f), Tooltip("Seconds at most from lifting the first Tap to touching down for the second.")]
        private float doubleTapMaxGapSeconds = 0.3f;

        [SerializeField, Min(0f), Tooltip("How far apart the two Taps of a Double-tap may be, in screen units.")]
        private float doubleTapMaxDistance = 0.1f;

        [SerializeField, Min(0f), Tooltip("How far a Flick's finger must move, in screen units.")]
        private float flickMinDistance = 0.08f;

        [SerializeField, Min(0f), Tooltip("Seconds a Flick may take at most, from touching down to lifting.")]
        private float flickMaxSeconds = 0.3f;

        [SerializeField, Min(0f), Tooltip("Seconds at most between the two fingers of a Two-finger tap touching down.")]
        private float twoFingerTapWindowSeconds = 0.15f;

        public GestureThresholds()
        {
        }

        public GestureThresholds(
            float tapMaxSeconds,
            float tapMaxMovement,
            float holdSeconds,
            float doubleTapMaxGapSeconds,
            float doubleTapMaxDistance,
            float flickMinDistance,
            float flickMaxSeconds,
            float twoFingerTapWindowSeconds)
        {
            this.tapMaxSeconds = tapMaxSeconds;
            this.tapMaxMovement = tapMaxMovement;
            this.holdSeconds = holdSeconds;
            this.doubleTapMaxGapSeconds = doubleTapMaxGapSeconds;
            this.doubleTapMaxDistance = doubleTapMaxDistance;
            this.flickMinDistance = flickMinDistance;
            this.flickMaxSeconds = flickMaxSeconds;
            this.twoFingerTapWindowSeconds = twoFingerTapWindowSeconds;
        }

        public float TapMaxSeconds => tapMaxSeconds;
        public float TapMaxMovement => tapMaxMovement;
        public float HoldSeconds => holdSeconds;
        public float DoubleTapMaxGapSeconds => doubleTapMaxGapSeconds;
        public float DoubleTapMaxDistance => doubleTapMaxDistance;
        public float FlickMinDistance => flickMinDistance;
        public float FlickMaxSeconds => flickMaxSeconds;
        public float TwoFingerTapWindowSeconds => twoFingerTapWindowSeconds;
    }
}
