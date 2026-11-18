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

        [SerializeField, Min(0f), Tooltip("Seconds at most from the last finger of a Two-finger double-tap's first tap lifting to the first finger of its second touching down.")]
        private float doubleTapMaxGapSeconds = 0.3f;

        [SerializeField, Min(0f), Tooltip("How far apart the centroids of a Two-finger double-tap's two taps may be, in screen units.")]
        private float doubleTapMaxDistance = 0.1f;

        [SerializeField, Min(0f), Tooltip("How far a Flick's finger must move, in screen units.")]
        private float flickMinDistance = 0.08f;

        [SerializeField, Min(0f), Tooltip("Seconds a Flick may take at most, from touching down to lifting.")]
        private float flickMaxSeconds = 0.3f;

        [SerializeField, Min(0f), Tooltip("Seconds at most between the two fingers of a Two-finger tap or Two-finger flick touching down.")]
        private float twoFingerTapWindowSeconds = 0.15f;

        [SerializeField, Min(0f), Tooltip("How far each finger of a Two-finger flick must move, in screen units.")]
        private float twoFingerFlickMinDistance = 0.08f;

        [SerializeField, Min(0f), Tooltip("Seconds a Two-finger flick may take at most, from the first finger touching down to the last lifting.")]
        private float twoFingerFlickMaxSeconds = 0.35f;

        [SerializeField, Range(0f, 180f), Tooltip("Degrees at most between the two fingers' movements: how parallel a Two-finger flick must be.")]
        private float twoFingerFlickMaxAngleDegrees = 35f;

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
            float twoFingerTapWindowSeconds,
            float twoFingerFlickMinDistance = 0.08f,
            float twoFingerFlickMaxSeconds = 0.35f,
            float twoFingerFlickMaxAngleDegrees = 35f)
        {
            this.tapMaxSeconds = tapMaxSeconds;
            this.tapMaxMovement = tapMaxMovement;
            this.holdSeconds = holdSeconds;
            this.doubleTapMaxGapSeconds = doubleTapMaxGapSeconds;
            this.doubleTapMaxDistance = doubleTapMaxDistance;
            this.flickMinDistance = flickMinDistance;
            this.flickMaxSeconds = flickMaxSeconds;
            this.twoFingerTapWindowSeconds = twoFingerTapWindowSeconds;
            this.twoFingerFlickMinDistance = twoFingerFlickMinDistance;
            this.twoFingerFlickMaxSeconds = twoFingerFlickMaxSeconds;
            this.twoFingerFlickMaxAngleDegrees = twoFingerFlickMaxAngleDegrees;
        }

        public float TapMaxSeconds => tapMaxSeconds;
        public float TapMaxMovement => tapMaxMovement;
        public float HoldSeconds => holdSeconds;
        public float DoubleTapMaxGapSeconds => doubleTapMaxGapSeconds;
        public float DoubleTapMaxDistance => doubleTapMaxDistance;
        public float FlickMinDistance => flickMinDistance;
        public float FlickMaxSeconds => flickMaxSeconds;
        public float TwoFingerTapWindowSeconds => twoFingerTapWindowSeconds;
        public float TwoFingerFlickMinDistance => twoFingerFlickMinDistance;
        public float TwoFingerFlickMaxSeconds => twoFingerFlickMaxSeconds;
        public float TwoFingerFlickMaxAngleDegrees => twoFingerFlickMaxAngleDegrees;
    }
}
