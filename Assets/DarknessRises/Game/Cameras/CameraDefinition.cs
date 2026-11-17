using UnityEngine;

namespace DarknessRises.Game.Cameras
{
    /// <summary>
    /// Authored framing for the fixed, tilted top-down camera that follows the character.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Camera Definition", fileName = "CameraDefinition", order = 20)]
    public sealed class CameraDefinition : ScriptableObject
    {
        [SerializeField, Range(10f, 90f), Tooltip("Degrees the camera looks down from the horizon. 90 is straight down.")]
        private float pitchDegrees = 60f;

        [SerializeField, Min(0.1f), Tooltip("Metres from the camera to the character.")]
        private float distance = 16f;

        [SerializeField, Range(10f, 90f), Tooltip("Vertical field of view in degrees.")]
        private float fieldOfView = 40f;

        [SerializeField, Tooltip("Seconds the camera takes to catch up with the character on each axis.")]
        private Vector3 followDamping = new(0.5f, 0.5f, 0.5f);

        public float PitchDegrees => pitchDegrees;
        public float Distance => distance;
        public float FieldOfView => fieldOfView;
        public Vector3 FollowDamping => followDamping;
    }
}
