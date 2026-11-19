using System;
using DarknessRises.Game.Controls;
using DarknessRises.Game.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// The crosshair on the HUD: a reticle where the player aims that kicks with every Shot, so the first tap of the
    /// game visibly lands. <see cref="Reticle"/> decides when it shows and how big; this puts it under the pointer.
    /// It isn't a raycast target, so it never takes a tap, and it touches the canvas only while it shows.
    /// </summary>
    /// <remarks>
    /// Runs after the Primary weapon, which reads the pointer and fires, so the crosshair is where this frame's Shot went.
    /// </remarks>
    [DefaultExecutionOrder(AfterPrimaryWeapon)]
    public sealed class CrosshairView : MonoBehaviour
    {
        private const int AfterPrimaryWeapon = 150;

        [SerializeField, Tooltip("The HUD area the crosshair moves in: stretched over the whole canvas.")]
        private RectTransform area;

        [SerializeField] private Image crosshair;

        [SerializeField, Tooltip("Where the player's pointer is and whether Fire is held.")]
        private PlayerControls controls;

        [SerializeField, Tooltip("While the Primary weapon is off (the character is dead), there's nothing to aim.")]
        private PrimaryWeaponView weaponView;

        private CrosshairSettings settings;
        private PrimaryWeapon weapon;
        private Reticle reticle;
        private Action onFired;
        private float shownAlpha = -1f;
        private float shownScale = -1f;
        private bool hidesCursor;

        /// <summary>Starts showing the crosshair as <paramref name="crosshairSettings"/> say, kicking with the Shots of <paramref name="primaryWeapon"/>.</summary>
        public void Show(CrosshairSettings crosshairSettings, PrimaryWeapon primaryWeapon)
        {
            settings = crosshairSettings ?? throw new ArgumentNullException(nameof(crosshairSettings));
            reticle = new Reticle(settings.KickScale, settings.KickSeconds, settings.TouchLingerSeconds, settings.FadeSeconds);
            crosshair.rectTransform.sizeDelta = new Vector2(settings.Size, settings.Size);
            crosshair.color = settings.Color;
            ShowWeapon(primaryWeapon);
            Apply();
        }

        /// <summary>Kicks with the Shots of <paramref name="primaryWeapon"/> from now on, after the player switches Primaries.</summary>
        public void ShowWeapon(PrimaryWeapon primaryWeapon)
        {
            onFired ??= OnFired;
            if (weapon != null)
            {
                weapon.Fired -= onFired;
            }

            weapon = primaryWeapon;
            if (weapon != null)
            {
                weapon.Fired += onFired;
            }
        }

        private void OnFired()
        {
            reticle.Fire();
        }

        private void Update()
        {
            if (reticle == null)
            {
                return;
            }

            // Nothing to aim while the game is paused (its controls are held) or the character is dead (its weapon is off).
            var canAim = !controls.IsHeld && weaponView.isActiveAndEnabled;
            if (!canAim)
            {
                reticle.Hide();
            }
            else
            {
                reticle.Tick(Time.deltaTime, !controls.IsTouchActive || controls.IsFireHeld);
            }

            UseCursor(canAim && !controls.IsTouchActive && settings.HideMouseCursor);
            Apply();
            if (reticle.IsVisible)
            {
                Place();
            }
        }

        private void OnDisable()
        {
            UseCursor(false);
        }

        private void OnDestroy()
        {
            if (weapon != null)
            {
                weapon.Fired -= onFired;
            }
        }

        // The crosshair stands in for the mouse cursor while the player aims with it.
        private void UseCursor(bool hide)
        {
            if (hide == hidesCursor)
            {
                return;
            }

            hidesCursor = hide;
            Cursor.visible = !hide;
        }

        private void Apply()
        {
            var alpha = reticle.Alpha;
            if (!Mathf.Approximately(alpha, shownAlpha))
            {
                shownAlpha = alpha;
                var visible = alpha > 0f;
                if (crosshair.enabled != visible)
                {
                    crosshair.enabled = visible;
                }

                var tint = settings.Color;
                tint.a *= alpha;
                crosshair.color = tint;
            }

            var scale = reticle.Scale;
            if (!Mathf.Approximately(scale, shownScale))
            {
                shownScale = scale;
                crosshair.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        // The HUD is a Screen Space Overlay canvas, so the pointer's screen point maps to it with no camera.
        private void Place()
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(area, controls.PointerPosition, null, out var local))
            {
                crosshair.rectTransform.anchoredPosition = local;
            }
        }
    }
}
