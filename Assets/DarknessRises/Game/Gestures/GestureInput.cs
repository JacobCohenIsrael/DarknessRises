using DarknessRises.Game.Controls;
using DarknessRises.Game.Grenades;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace DarknessRises.Game.Gestures
{
    /// <summary>
    /// Feeds the screen's touches through the <see cref="GestureRecognizer"/> and presses the matching controls of a
    /// <see cref="GestureDevice"/>, whose controls are bound to the Gameplay actions. Gameplay only ever reads those
    /// actions (through <see cref="PlayerControls"/>), never the touches.
    /// </summary>
    /// <remarks>
    /// A touch that starts on a UI control (the pause button) is left to the UI and never becomes a Gesture.
    /// One-off commands (a Tap's Fire, Reload, Relocate) are pressed for one frame; a Hold keeps Fire pressed.
    /// While the Grenade button is armed, a touch in the Room (a Tap, or one that stays long enough to start a Hold)
    /// presses Grenade instead of Fire, with the Grenade aimed at the touched point, so the touch that throws never fires.
    /// Runs early, so the device's state is queued before the rest of the frame reads input.
    /// </remarks>
    [DefaultExecutionOrder(-100)]
    public sealed class GestureInput : MonoBehaviour, IGestureSink
    {
        private const int MostFingers = 10;

        private readonly UiPointerFilter ui = new();
        private readonly int[] uiFingers = new int[MostFingers];
        private int uiFingerCount;

        [SerializeField, Tooltip("Aims a Grenade thrown with the Grenade button at the tapped point.")]
        private PlayerControls controls;

        private GestureDefinition definition;
        private GrenadeButtonRules grenadeButton;
        private GestureRecognizer recognizer;
        private GestureDevice device;

        private bool isHoldingFire;
        private uint pulses;

        // One-frame presses asked for from outside a Gesture (the Weapon button), sent with the next state.
        private uint queuedPulses;
        private Vector2 relocate;
        private GestureDeviceState sent;

        /// <summary>Starts recognizing Gestures as <paramref name="gestures"/> defines them.</summary>
        public void Use(GestureDefinition gestures)
        {
            definition = gestures;
            recognizer = GestureRecognizer.WithCoreGestures(definition.Thresholds, this);
        }

        /// <summary>Lets an armed <paramref name="button"/> turn the next Room tap into a Grenade throw.</summary>
        public void UseGrenadeButton(GrenadeButtonRules button)
        {
            grenadeButton = button;
        }

        /// <summary>
        /// Presses the Next Weapon command for a frame: the on-screen Weapon button was tapped. It goes through the
        /// Gesture device like every touch command, so it drives the same named action as the mouse wheel.
        /// </summary>
        public void PressNextWeapon()
        {
            queuedPulses |= 1u << GestureDeviceState.NextWeaponBit;
        }

        void IGestureSink.OnGesture(in Gesture gesture)
        {
            switch (GestureCommands.For(gesture, definition.TwoFingerFlickAxisHalfAngleDegrees))
            {
                case GestureCommand.FireOnce when grenadeButton != null && grenadeButton.TapRoom() == RoomTapOutcome.ThrowGrenade:
                    // Gesture positions are in screen units (the screen's shorter side is 1): back to pixels.
                    controls.AimGrenadeAt(gesture.Position * ShorterScreenSide());
                    pulses |= 1u << GestureDeviceState.GrenadeBit;
                    break;
                case GestureCommand.FireOnce:
                    pulses |= 1u << GestureDeviceState.FireBit;
                    break;
                case GestureCommand.FireHold when grenadeButton != null && grenadeButton.TapRoom() == RoomTapOutcome.ThrowGrenade:
                    controls.AimGrenadeAt(gesture.Position * ShorterScreenSide());
                    pulses |= 1u << GestureDeviceState.GrenadeBit;
                    break;
                case GestureCommand.FireHold:
                    isHoldingFire = true;
                    break;
                case GestureCommand.FireRelease:
                    isHoldingFire = false;
                    break;
                case GestureCommand.Grenade:
                    pulses |= 1u << GestureDeviceState.GrenadeBit;
                    break;
                case GestureCommand.Reload:
                    pulses |= 1u << GestureDeviceState.ReloadBit;
                    break;
                case GestureCommand.Flare:
                    pulses |= 1u << GestureDeviceState.FlareBit;
                    break;
                case GestureCommand.SlowMo:
                    pulses |= 1u << GestureDeviceState.SlowMoBit;
                    break;
                case GestureCommand.Relocate:
                    relocate = gesture.Direction;
                    break;
            }
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            device = InputSystem.AddDevice<GestureDevice>();
            sent = default;
        }

        private void OnDisable()
        {
            if (device != null && device.added)
            {
                InputSystem.RemoveDevice(device);
            }

            device = null;
            EnhancedTouchSupport.Disable();
            recognizer?.Reset();
            isHoldingFire = false;
            queuedPulses = 0;
            uiFingerCount = 0;
        }

        private void Update()
        {
            if (recognizer == null || device == null)
            {
                return;
            }

            // Last frame's one-frame presses are let go by the state sent below.
            pulses = queuedPulses;
            queuedPulses = 0;
            relocate = Vector2.zero;

            var touches = Touch.activeTouches;
            var fingers = 0;
            for (var i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch.phase == TouchPhase.Began && ui.IsOverControl(touch.screenPosition))
                {
                    AddUiFinger(touch.touchId);
                }

                if (!IsUiFinger(touch.touchId))
                {
                    fingers++;
                }
            }

            var now = Time.unscaledTime;
            var screenUnit = 1f / ShorterScreenSide();
            for (var i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                var phase = Phase(touch.phase);
                if (IsUiFinger(touch.touchId))
                {
                    if (phase == TouchSamplePhase.Ended || phase == TouchSamplePhase.Canceled)
                    {
                        RemoveUiFinger(touch.touchId);
                    }

                    continue;
                }

                recognizer.Process(new TouchSample(touch.touchId, phase, touch.screenPosition * screenUnit, now, fingers));
            }

            recognizer.Tick(now);
            Send();
        }

        private void Send()
        {
            var state = new GestureDeviceState
            {
                buttons = (isHoldingFire ? 1u << GestureDeviceState.FireBit : 0u) | pulses,
                relocate = relocate,
            };

            if (state.Equals(sent))
            {
                return;
            }

            sent = state;
            InputSystem.QueueStateEvent(device, state);
        }

        private static int ShorterScreenSide()
        {
            return Mathf.Max(1, Mathf.Min(Screen.width, Screen.height));
        }

        private static TouchSamplePhase Phase(TouchPhase phase)
        {
            switch (phase)
            {
                case TouchPhase.Began:
                    return TouchSamplePhase.Began;
                case TouchPhase.Moved:
                    return TouchSamplePhase.Moved;
                case TouchPhase.Ended:
                    return TouchSamplePhase.Ended;
                case TouchPhase.Canceled:
                    return TouchSamplePhase.Canceled;
                default:
                    return TouchSamplePhase.Stationary;
            }
        }

        private void AddUiFinger(int touchId)
        {
            if (!IsUiFinger(touchId) && uiFingerCount < MostFingers)
            {
                uiFingers[uiFingerCount++] = touchId;
            }
        }

        private bool IsUiFinger(int touchId)
        {
            for (var i = 0; i < uiFingerCount; i++)
            {
                if (uiFingers[i] == touchId)
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveUiFinger(int touchId)
        {
            for (var i = 0; i < uiFingerCount; i++)
            {
                if (uiFingers[i] == touchId)
                {
                    uiFingers[i] = uiFingers[--uiFingerCount];
                    return;
                }
            }
        }
    }
}
