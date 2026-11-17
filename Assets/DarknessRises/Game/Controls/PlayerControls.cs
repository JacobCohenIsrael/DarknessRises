using UnityEngine;
using UnityEngine.InputSystem;

namespace DarknessRises.Game.Controls
{
    /// <summary>
    /// The player's commands, read from the named actions in the game's source-controlled input actions asset.
    /// Gameplay reads commands here and never polls devices directly.
    /// </summary>
    public sealed class PlayerControls : MonoBehaviour
    {
        [SerializeField, Tooltip("Gameplay/Aim: the pointer's screen position.")]
        private InputActionReference aim;

        [SerializeField, Tooltip("Gameplay/Fire: held to keep firing.")]
        private InputActionReference fire;

        [SerializeField, Tooltip("Gameplay/Reload.")]
        private InputActionReference reload;

        [SerializeField, Tooltip("The camera the player sees the Room through, used to turn the pointer into an aim point.")]
        private Camera viewCamera;

        public bool IsFireHeld => fire.action.IsPressed();
        public bool WasReloadPressedThisFrame => reload.action.WasPressedThisFrame();

        /// <summary>The point in the Room under the pointer, on the horizontal plane at <paramref name="height"/>.</summary>
        public bool TryGetAimPoint(float height, out Vector3 aimPoint)
        {
            var ray = viewCamera.ScreenPointToRay(aim.action.ReadValue<Vector2>());
            return AimPlane.TryIntersect(ray, height, out aimPoint);
        }

        private void OnEnable()
        {
            aim.action.Enable();
            fire.action.Enable();
            reload.action.Enable();
        }

        private void OnDisable()
        {
            aim.action.Disable();
            fire.action.Disable();
            reload.action.Disable();
        }
    }
}
