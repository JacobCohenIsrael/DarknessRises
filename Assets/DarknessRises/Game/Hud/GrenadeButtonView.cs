using DarknessRises.Game.Controls;
using DarknessRises.Game.Grenades;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// The on-screen Grenade button, touch only. Feeds the button's presses, holds and drags into the
    /// <see cref="GrenadeButtonRules"/>, draws the button where the rules put it (pulsing while armed, greyed with no
    /// charges) and shows the Grenade charges on it. The Room tap that throws is handled by the Gesture input, which
    /// shares the same rules.
    /// </summary>
    /// <remarks>
    /// Shown while the player is on touch; on keyboard and mouse it's hidden and the HUD's corner charges show instead,
    /// so the charges never appear twice. The button is an interactable UI control, so a press on it never fires and
    /// never becomes a Gesture (<see cref="UiPointerFilter"/>). Its dropped position is a per-device player preference,
    /// saved normalized to the safe area.
    /// </remarks>
    public sealed class GrenadeButtonView : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IInitializePotentialDragHandler, IDragHandler
    {
        private const string PositionXKey = "DarknessRises.GrenadeButton.X";
        private const string PositionYKey = "DarknessRises.GrenadeButton.Y";

        [SerializeField, Tooltip("Tells whether the player is on touch.")]
        private PlayerControls controls;

        [SerializeField, Tooltip("The HUD canvas, for its scale.")]
        private Canvas canvas;

        [SerializeField, Tooltip("This button, anchored at the bottom left of its parent, which covers the canvas.")]
        private RectTransform button;

        [SerializeField, Tooltip("Hides the button, and stops it catching touches, on keyboard and mouse.")]
        private CanvasGroup visibility;

        [SerializeField, Tooltip("The button's face, coloured by its state.")]
        private Image face;

        [SerializeField, Tooltip("The Grenade charges drawn on the button.")]
        private GrenadeHudView charges;

        [SerializeField, Tooltip("The HUD's corner charges, shown instead of the button on keyboard and mouse.")]
        private GameObject cornerCharges;

        private GrenadeButtonSettings settings;
        private GrenadeCharges grenadeCharges;
        private bool isShown;
        private int pressingPointer;
        private Rect fittedSafeArea;
        private float fittedScale;
        private bool wasArmed;
        private float armedAt;

        /// <summary>The button's rules, shared with the Gesture input so an armed button's Room tap throws.</summary>
        public GrenadeButtonRules Rules { get; private set; }

        public void Show(GrenadeCharges carried, GrenadeButtonSettings buttonSettings)
        {
            grenadeCharges = carried;
            settings = buttonSettings;
            Rules ??= new GrenadeButtonRules(settings.HoldSeconds, LoadPosition(settings.DefaultPosition));
            fittedScale = 0f;
            charges.Show(carried);
            SetShown(controls.IsTouchActive);
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (Rules == null || !isShown || Rules.IsPressed)
            {
                return;
            }

            pressingPointer = eventData.pointerId;
            Rules.Press(Time.unscaledTime);
        }

        void IInitializePotentialDragHandler.OnInitializePotentialDrag(PointerEventData eventData)
        {
            // A picked-up button follows the finger from the first pixel.
            eventData.useDragThreshold = false;
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (Rules == null || !Rules.IsPressed || eventData.pointerId != pressingPointer)
            {
                return;
            }

            Rules.Tick(Time.unscaledTime);
            Rules.Drag(eventData.position);
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            if (Rules == null || !Rules.IsPressed || eventData.pointerId != pressingPointer)
            {
                return;
            }

            if (Rules.Release(Time.unscaledTime, grenadeCharges.Charges))
            {
                SavePosition(Rules.NormalizedPosition);
            }
        }

        private void Update()
        {
            if (Rules == null)
            {
                return;
            }

            if (controls.IsTouchActive != isShown)
            {
                SetShown(controls.IsTouchActive);
            }

            if (!isShown)
            {
                return;
            }

            FitToScreen();
            Rules.Tick(Time.unscaledTime);
            Rules.UpdateCharges(grenadeCharges.Charges);
            Draw();
        }

        private void SetShown(bool shown)
        {
            isShown = shown;
            visibility.alpha = shown ? 1f : 0f;
            visibility.blocksRaycasts = shown;
            visibility.interactable = shown;
            cornerCharges.SetActive(!shown);
            if (!shown)
            {
                Rules.Cancel();
            }
        }

        // The safe area and the canvas scale change with the resolution and orientation; the normalized position doesn't.
        private void FitToScreen()
        {
            var safeArea = Screen.safeArea;
            var scale = canvas.scaleFactor;
            if (safeArea == fittedSafeArea && Mathf.Approximately(scale, fittedScale))
            {
                return;
            }

            fittedSafeArea = safeArea;
            fittedScale = scale;
            Rules.Fit(safeArea, Vector2.one * (settings.Size * scale));
        }

        private void Draw()
        {
            var scale = canvas.scaleFactor;
            button.anchoredPosition = Rules.ScreenPosition / scale;
            button.sizeDelta = Vector2.one * settings.Size;
            button.localScale = Vector3.one * (Rules.State == GrenadeButtonState.Moving ? settings.MovingScale : 1f);
            face.color = FaceColor();
            wasArmed = Rules.IsArmed && grenadeCharges.Charges > 0;
        }

        private Color FaceColor()
        {
            if (grenadeCharges.Charges <= 0)
            {
                return settings.EmptyColor;
            }

            if (!Rules.IsArmed)
            {
                return settings.IdleColor;
            }

            if (!wasArmed)
            {
                armedAt = Time.unscaledTime;
            }

            // Starts at the armed colour the moment it's armed, and pulses toward Idle and back.
            var pulse = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * (Time.unscaledTime - armedAt) / settings.ArmedPulseSeconds);
            return Color.Lerp(settings.ArmedColor, settings.IdleColor, pulse);
        }

        private static Vector2 LoadPosition(Vector2 fallback)
        {
            if (!PlayerPrefs.HasKey(PositionXKey) || !PlayerPrefs.HasKey(PositionYKey))
            {
                return fallback;
            }

            return new Vector2(PlayerPrefs.GetFloat(PositionXKey), PlayerPrefs.GetFloat(PositionYKey));
        }

        private static void SavePosition(Vector2 normalized)
        {
            PlayerPrefs.SetFloat(PositionXKey, normalized.x);
            PlayerPrefs.SetFloat(PositionYKey, normalized.y);
            PlayerPrefs.Save();
        }
    }
}
