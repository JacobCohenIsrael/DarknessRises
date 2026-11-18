using DarknessRises.Game.Controls;
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
    /// Runs early, so the device's state is queued before the rest of the frame reads input.
    /// </remarks>
    [DefaultExecutionOrder(-100)]
    public sealed class GestureInput : MonoBehaviour, IGestureSink
    {
        private const int MostFingers = 10;

        private readonly UiPointerFilter ui = new();
        private readonly int[] uiFingers = new int[MostFingers];
        private int uiFingerCount;

        private GestureDefinition definition;
        private GestureRecognizer recognizer;
        private GestureDevice device;

        private bool isHoldingFire;
        private uint pulses;
        private Vector2 relocate;
        private GestureDeviceState sent;

        /// <summary>Starts recognizing Gestures as <paramref name="gestures"/> defines them.</summary>
        public void Use(GestureDefinition gestures)
        {
            definition = gestures;
            recognizer = GestureRecognizer.WithCoreGestures(definition.Thresholds, this);
        }

        void IGestureSink.OnGesture(in Gesture gesture)
        {
            switch (GestureCommands.For(gesture, definition.FlickAxisHalfAngleDegrees))
            {
                case GestureCommand.FireOnce:
                    pulses |= 1u << GestureDeviceState.FireBit;
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
            uiFingerCount = 0;
        }

        private void Update()
        {
            if (recognizer == null || device == null)
            {
                return;
            }

            // Last frame's one-frame presses are let go by the state sent below.
            pulses = 0;
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
            var screenUnit = 1f / Mathf.Max(1, Mathf.Min(Screen.width, Screen.height));
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
