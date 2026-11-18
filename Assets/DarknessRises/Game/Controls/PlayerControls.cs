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

        [SerializeField, Tooltip("Gameplay/Flare: triggers the Flare Skill.")]
        private InputActionReference flare;

        [SerializeField, Tooltip("Gameplay/SlowMo: triggers the Slow-mo Skill.")]
        private InputActionReference slowMo;

        [SerializeField, Tooltip("Gameplay/Grenade: throws a Grenade at the aim point.")]
        private InputActionReference grenade;

        [SerializeField, Tooltip("The camera the player sees the Room through, used to turn the pointer into an aim point.")]
        private Camera viewCamera;

        // While held (the game is paused) every command reads as none, and aiming stays where the pointer last was.
        private bool isHeld;
        private Vector2 lastPointer;

        public bool IsFireHeld => fire.action.IsPressed();
        public bool WasReloadPressedThisFrame => reload.action.WasPressedThisFrame();
        public bool WasFlarePressedThisFrame => flare.action.WasPressedThisFrame();
        public bool WasSlowMoPressedThisFrame => slowMo.action.WasPressedThisFrame();
        public bool WasGrenadePressedThisFrame => grenade.action.WasPressedThisFrame();

        /// <summary>The point in the Room under the pointer, on the horizontal plane at <paramref name="height"/>.</summary>
        public bool TryGetAimPoint(float height, out Vector3 aimPoint)
        {
            if (aim.action.enabled)
            {
                lastPointer = aim.action.ReadValue<Vector2>();
            }

            var ray = viewCamera.ScreenPointToRay(lastPointer);
            return AimPlane.TryIntersect(ray, height, out aimPoint);
        }

        /// <summary>
        /// Holds the player's commands while the game is paused: the Gameplay actions are disabled, so nothing fires,
        /// reloads, throws or triggers a Skill, and a held trigger counts as released. Releasing turns them back on.
        /// </summary>
        public void Hold(bool held)
        {
            isHeld = held;
            if (held)
            {
                DisableActions();
            }
            else if (isActiveAndEnabled)
            {
                EnableActions();
            }
        }

        private void OnEnable()
        {
            if (!isHeld)
            {
                EnableActions();
            }
        }

        private void OnDisable()
        {
            DisableActions();
        }

        private void EnableActions()
        {
            aim.action.Enable();
            fire.action.Enable();
            reload.action.Enable();
            flare.action.Enable();
            slowMo.action.Enable();
            grenade.action.Enable();
        }

        private void DisableActions()
        {
            aim.action.Disable();
            fire.action.Disable();
            reload.action.Disable();
            flare.action.Disable();
            slowMo.action.Disable();
            grenade.action.Disable();
        }
    }
}
