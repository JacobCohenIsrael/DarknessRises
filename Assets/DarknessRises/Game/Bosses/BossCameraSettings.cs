using System;
using UnityEngine;

namespace DarknessRises.Game.Bosses
{
    /// <summary>How the Boss Room's camera frames the whole fight and every Position.</summary>
    [Serializable]
    public sealed class BossCameraSettings
    {
        [SerializeField, Range(10f, 90f), Tooltip("Degrees the camera looks down from the horizon.")]
        private float pitchDegrees = 62f;

        [SerializeField, Range(10f, 90f), Tooltip("Vertical field of view in degrees.")]
        private float fieldOfView = 40f;

        [SerializeField, Min(0f), Tooltip("Metres of floor kept in view around each Position, so the stomp shadows show whole.")]
        private float marginMetres = 3f;

        [SerializeField, Min(0.1f), Tooltip("The camera never comes closer than this, in metres.")]
        private float minDistance = 16f;

        [SerializeField, Min(0.1f), Tooltip("Width over height the framing assumes when no camera is showing yet.")]
        private float fallbackAspect = 16f / 9f;

        public float PitchDegrees => pitchDegrees;
        public float FieldOfView => fieldOfView;
        public float MarginMetres => marginMetres;
        public float MinDistance => minDistance;
        public float FallbackAspect => fallbackAspect;
    }
}
