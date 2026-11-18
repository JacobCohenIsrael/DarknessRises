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

        [SerializeField, Tooltip(
            "Gameplay/Relocate: a direction (WASD or the arrow keys) that picks the Position to dash to in a Boss fight.")]
        private InputActionReference relocate;

        [SerializeField, Tooltip("The camera the player sees the Room through, used to turn the pointer into an aim point.")]
        private Camera viewCamera;

        // The direction last pressed, and the frame it was pressed in. Only that frame's press counts.
        private Vector2 relocateHeld;
        private Vector2 relocatePressed;
        private int relocatePressedFrame = -1;

        public bool IsFireHeld => fire.action.IsPressed();
        public bool WasReloadPressedThisFrame => reload.action.WasPressedThisFrame();
        public bool WasFlarePressedThisFrame => flare.action.WasPressedThisFrame();
        public bool WasSlowMoPressedThisFrame => slowMo.action.WasPressedThisFrame();
        public bool WasGrenadePressedThisFrame => grenade.action.WasPressedThisFrame();

        /// <summary>
        /// The direction the player pressed this frame to Relocate, if any: x is screen right, y is screen up.
        /// Only newly pressed keys count, so holding one and pressing another picks the new one's direction.
        /// </summary>
        public bool TryGetRelocateDirection(out Vector2 direction)
        {
            direction = relocatePressed;
            return relocatePressedFrame == Time.frameCount && relocatePressed != Vector2.zero;
        }

        /// <summary>Forgets a Relocate press, so a key pressed before Relocate opened doesn't count.</summary>
        public void ClearRelocate()
        {
            relocatePressedFrame = -1;
            relocatePressed = Vector2.zero;
        }

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
            flare.action.Enable();
            slowMo.action.Enable();
            grenade.action.Enable();
            if (relocate != null)
            {
                relocate.action.performed += OnRelocateChanged;
                relocate.action.canceled += OnRelocateChanged;
                relocate.action.Enable();
            }
        }

        private void OnDisable()
        {
            aim.action.Disable();
            fire.action.Disable();
            reload.action.Disable();
            flare.action.Disable();
            slowMo.action.Disable();
            grenade.action.Disable();
            if (relocate != null)
            {
                relocate.action.performed -= OnRelocateChanged;
                relocate.action.canceled -= OnRelocateChanged;
                relocate.action.Disable();
            }
        }

        // Keeps the axes that were just pressed (or flipped), not the ones already held, so letting go never relocates.
        private void OnRelocateChanged(InputAction.CallbackContext context)
        {
            var value = context.ReadValue<Vector2>();
            var pressed = new Vector2(NewlyPressed(value.x, relocateHeld.x), NewlyPressed(value.y, relocateHeld.y));
            relocateHeld = value;
            if (pressed != Vector2.zero)
            {
                relocatePressed = pressed;
                relocatePressedFrame = Time.frameCount;
            }
        }

        private static float NewlyPressed(float now, float before)
        {
            var sign = Mathf.Abs(now) < 0.5f ? 0f : Mathf.Sign(now);
            var beforeSign = Mathf.Abs(before) < 0.5f ? 0f : Mathf.Sign(before);
            return sign != beforeSign ? sign : 0f;
        }
    }
}
