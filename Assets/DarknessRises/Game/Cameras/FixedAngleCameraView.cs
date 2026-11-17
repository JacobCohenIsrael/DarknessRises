using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace DarknessRises.Game.Cameras
{
    /// <summary>
    /// Points a Cinemachine camera at its follow target (the point just ahead of the character) from the authored
    /// fixed, tilted top-down angle.
    /// Cinemachine owns the camera; this only hands it the framing.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera), typeof(CinemachineFollow))]
    public sealed class FixedAngleCameraView : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private CinemachineFollow follow;

        public void Frame(CameraDefinition definition, Transform followTarget)
        {
            cinemachineCamera.Follow = followTarget;

            var lens = cinemachineCamera.Lens;
            lens.FieldOfView = definition.FieldOfView;
            cinemachineCamera.Lens = lens;

            follow.TrackerSettings.BindingMode = BindingMode.WorldSpace;
            follow.TrackerSettings.PositionDamping = definition.FollowDamping;
            follow.FollowOffset = FixedAngleFraming.FollowOffset(definition.PitchDegrees, definition.Distance);

            transform.SetPositionAndRotation(
                followTarget.position + follow.FollowOffset,
                FixedAngleFraming.Rotation(definition.PitchDegrees));
        }

        private void Reset()
        {
            cinemachineCamera = GetComponent<CinemachineCamera>();
            follow = GetComponent<CinemachineFollow>();
        }
    }
}
