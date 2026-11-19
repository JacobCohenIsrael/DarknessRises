using System;
using DarknessRises.Game.Gestures;
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

        // While held (the game is paused) every command reads as none, and aiming stays where the pointer last was.
        private bool isHeld;
        private Vector2 lastPointer;

        // The direction last pressed, and the frame it was pressed in. Only that frame's press counts.
        private Vector2 relocateHeld;
        private Vector2 relocatePressed;
        private int relocatePressedFrame = -1;

        // A press of Fire that started on a UI control (the pause button) is the UI's, until it's let go.
        private readonly UiPointerFilter ui = new();
        private bool isFirePressOnUi;

        // A screen point the next Grenade is thrown at instead of the pointer: a Room tap with the Grenade button armed.
        private bool hasGrenadeAim;
        private Vector2 grenadeAim;

        private Action<InputAction.CallbackContext> onCommand;

        /// <summary>
        /// Whether Fire is held. A click or tap that starts on a UI control (the pause button) never counts.
        /// </summary>
        public bool IsFireHeld
        {
            get
            {
                var action = fire.action;
                if (action.WasPressedThisFrame())
                {
                    isFirePressOnUi = ui.IsOverControl(lastPointer);
                }

                return action.IsPressed() && !isFirePressOnUi;
            }
        }

        /// <summary>
        /// Whether the player's last command or aim came from touch (the Touch control scheme) rather than keyboard and mouse.
        /// Aim assist only helps on touch.
        /// </summary>
        public bool IsTouchActive { get; private set; }

        /// <summary>Where the player's pointer last was on screen, in pixels: the mouse, or the finger that last aimed.</summary>
        public Vector2 PointerPosition => lastPointer;

        /// <summary>Whether the commands are held because the game is paused.</summary>
        public bool IsHeld => isHeld;

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
            if (aim.action.enabled)
            {
                lastPointer = aim.action.ReadValue<Vector2>();
            }

            var ray = viewCamera.ScreenPointToRay(lastPointer);
            return AimPlane.TryIntersect(ray, height, out aimPoint);
        }

        /// <summary>
        /// Aims the next Grenade at <paramref name="screenPoint"/> instead of the pointer. The touch Grenade button
        /// sets it with the tapped point when it presses Grenade, so the throw lands where the player tapped even if
        /// another finger touches the screen before the Grenade leaves the hand.
        /// </summary>
        public void AimGrenadeAt(Vector2 screenPoint)
        {
            grenadeAim = screenPoint;
            hasGrenadeAim = true;
        }

        /// <summary>Forgets a Grenade aim that no throw used (the Grenade command didn't start a throw).</summary>
        public void ForgetGrenadeAim()
        {
            hasGrenadeAim = false;
        }

        /// <summary>
        /// Where a Grenade leaving the hand now lands: the point the Grenade button's tap aimed it at, used once,
        /// or else the aim point under the pointer (<see cref="TryGetAimPoint"/>), as with a right-click.
        /// </summary>
        public bool TryGetGrenadeAimPoint(float height, out Vector3 aimPoint)
        {
            if (!hasGrenadeAim)
            {
                return TryGetAimPoint(height, out aimPoint);
            }

            hasGrenadeAim = false;
            return AimPlane.TryIntersect(viewCamera.ScreenPointToRay(grenadeAim), height, out aimPoint);
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
            // Until the first command says otherwise, a device with a touchscreen and no mouse (a phone) is on touch.
            IsTouchActive |= Touchscreen.current != null && Mouse.current == null;
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
            onCommand ??= OnCommand;
            aim.action.performed -= onCommand;
            fire.action.started -= onCommand;
            aim.action.performed += onCommand;
            fire.action.started += onCommand;
            aim.action.Enable();
            fire.action.Enable();
            reload.action.Enable();
            flare.action.Enable();
            slowMo.action.Enable();
            grenade.action.Enable();
            if (relocate != null)
            {
                // Never subscribed twice, whichever of enabling and releasing the hold comes first.
                relocate.action.performed -= OnRelocateChanged;
                relocate.action.canceled -= OnRelocateChanged;
                relocate.action.performed += OnRelocateChanged;
                relocate.action.canceled += OnRelocateChanged;
                relocate.action.Enable();
            }
        }

        private void DisableActions()
        {
            if (onCommand != null)
            {
                aim.action.performed -= onCommand;
                fire.action.started -= onCommand;
            }

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
                relocateHeld = Vector2.zero;
                ClearRelocate();
            }
        }

        private void OnCommand(InputAction.CallbackContext context)
        {
            var device = context.control?.device;
            IsTouchActive = device is Touchscreen || device is GestureDevice;
            if (!IsTouchActive)
            {
                hasGrenadeAim = false;
            }
        }

        // Keeps the axes that were just pressed (or flipped), not the ones already held, so letting go never relocates.
        // A Flick's direction is taken whole: it's already a single, fresh press.
        private void OnRelocateChanged(InputAction.CallbackContext context)
        {
            var value = context.ReadValue<Vector2>();
            if (context.control?.device is GestureDevice)
            {
                IsTouchActive = true;
                if (value != Vector2.zero)
                {
                    relocatePressed = value;
                    relocatePressedFrame = Time.frameCount;
                }

                return;
            }

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
