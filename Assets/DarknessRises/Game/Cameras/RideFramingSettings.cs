using System;
using UnityEngine;

namespace DarknessRises.Game.Cameras
{
    /// <summary>
    /// One framing of the Ride camera: how steeply it looks down, how far it stands back, its lens, and how far it may
    /// swing off north to look where the action is.
    /// </summary>
    [Serializable]
    public sealed class RideFramingSettings
    {
        [SerializeField, Range(10f, 90f), Tooltip(
            "Degrees the camera looks down from the horizon. 90 is straight down. Lower is more dramatic but hides more behind Obstacles; " +
            "keep it well above half the field of view, or taps near the top of the screen miss the floor.")]
        private float pitchDegrees = 60f;

        [SerializeField, Min(0.1f), Tooltip("Metres from the camera to the point it looks at.")]
        private float distance = 16f;

        [SerializeField, Range(10f, 90f), Tooltip("Vertical field of view in degrees.")]
        private float fieldOfView = 40f;

        [SerializeField, Range(0f, 80f), Tooltip(
            "Most degrees the camera swings off north, to the east or the west. The Rooms are built to be seen from the south, " +
            "so a large swing looks past their cut-down south walls into the side walls. 0 never swings.")]
        private float swingDegrees;

        public RideFramingSettings()
        {
        }

        public RideFramingSettings(float pitchDegrees, float distance, float fieldOfView, float swingDegrees)
        {
            this.pitchDegrees = pitchDegrees;
            this.distance = distance;
            this.fieldOfView = fieldOfView;
            this.swingDegrees = swingDegrees;
        }

        public float PitchDegrees => pitchDegrees;
        public float Distance => distance;
        public float FieldOfView => fieldOfView;
        public float SwingDegrees => swingDegrees;
    }
}
