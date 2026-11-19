using System;
using DarknessRises.Game.Controls;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Weapons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// Shows the first-run hint on the HUD: "tap to shoot" with an arrow, bobbing over the first Enemy that comes at
    /// the character, until the player fires or that Enemy dies. <see cref="ShootHint"/> decides whether and when;
    /// this follows the Enemy on screen. Its text is set once, when it shows, and nothing in it is a raycast target.
    /// </summary>
    public sealed class ShootHintView : MonoBehaviour
    {
        // Remembers on this device that the hint was shown, for a hint authored to show once per install.
        private const string ShownKey = "DarknessRises.ShootHint.Shown";

        [SerializeField, Tooltip("The HUD area the hint moves in: stretched over the whole canvas.")]
        private RectTransform area;

        [SerializeField] private CanvasGroup group;

        [SerializeField, Tooltip("Holds the text and the arrow. Its pivot is the arrow's tip.")]
        private RectTransform marker;

        [SerializeField] private TMP_Text label;

        [SerializeField, Min(0f), Tooltip("The gap between the arrow and the text above it, in HUD units.")]
        private float labelGap = 8f;

        [SerializeField, Tooltip("Points down at the Enemy.")]
        private Image arrow;

        [SerializeField, Tooltip("The camera the player sees the Room through.")]
        private Camera worldCamera;

        [SerializeField, Tooltip("Tells whether the player is on touch or on a mouse, for the hint's wording.")]
        private PlayerControls controls;

        private ShootHintSettings settings;
        private ShootHint hint;
        private EnemyView target;
        private PrimaryWeapon weapon;
        private Action onFired;
        private float alpha;
        private float secondsShown;
        private float labelHalfWidth;
        private float labelHeight;

        /// <summary>Whether the hint is on screen, fading or not.</summary>
        public bool IsVisible => alpha > 0f;

        /// <summary>Gets the hint ready to show as <paramref name="hintSettings"/> say. Nothing shows until <see cref="Offer"/>.</summary>
        public void Use(ShootHintSettings hintSettings)
        {
            settings = hintSettings ?? throw new ArgumentNullException(nameof(hintSettings));
            hint = new ShootHint(settings.Frequency, PlayerPrefs.GetInt(ShownKey, 0) != 0);
            label.fontSize = settings.FontSize;
            label.color = settings.Color;
            arrow.color = settings.Color;
            arrow.rectTransform.sizeDelta = new Vector2(settings.ArrowSize, settings.ArrowSize);
            label.rectTransform.anchoredPosition = new Vector2(0f, settings.ArrowSize + labelGap);
            SetAlpha(0f);
        }

        /// <summary>
        /// An Enemy came at the character in Level <paramref name="levelIndex"/> of the Run. If the hint is due, it
        /// shows over <paramref name="enemy"/> until the player fires <paramref name="primaryWeapon"/> or the Enemy dies.
        /// </summary>
        public void Offer(EnemyView enemy, int levelIndex, PrimaryWeapon primaryWeapon)
        {
            if (hint == null || !hint.TryShow(levelIndex))
            {
                return;
            }

            target = enemy;
            secondsShown = 0f;
            label.text = controls.IsTouchActive ? settings.TouchText : settings.MouseText;

            // Measured once, to keep the text on screen while it follows the Enemy.
            labelHalfWidth = label.preferredWidth * 0.5f;
            labelHeight = label.preferredHeight;
            onFired ??= OnFired;
            weapon = primaryWeapon;
            weapon.Fired += onFired;
            if (settings.Frequency == ShootHintFrequency.OncePerInstall)
            {
                PlayerPrefs.SetInt(ShownKey, 1);
                PlayerPrefs.Save();
            }
        }

        private void OnFired()
        {
            Dismiss();
        }

        private void Dismiss()
        {
            hint.Dismiss();
            if (weapon != null)
            {
                weapon.Fired -= onFired;
                weapon = null;
            }
        }

        private void Update()
        {
            if (hint == null || (!hint.IsShowing && alpha <= 0f))
            {
                return;
            }

            var deltaSeconds = Time.deltaTime;
            if (hint.IsShowing && (target == null || !target.Enemy.IsAlive))
            {
                Dismiss();
            }

            var step = settings.FadeSeconds > 0f ? deltaSeconds / settings.FadeSeconds : 1f;
            SetAlpha(Mathf.MoveTowards(alpha, hint.IsShowing ? 1f : 0f, step));
            if (alpha <= 0f || target == null)
            {
                return;
            }

            secondsShown += deltaSeconds;
            Place();
        }

        private void OnDestroy()
        {
            if (weapon != null)
            {
                weapon.Fired -= onFired;
            }
        }

        // The arrow's tip sits on the Enemy, bobbing up and back. The HUD is an overlay canvas, so no camera maps to it.
        private void Place()
        {
            var screen = worldCamera.WorldToScreenPoint(target.transform.position + settings.WorldOffset);
            if (screen.z <= 0f ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, null, out var local))
            {
                return;
            }

            local.y += settings.BobDistance * Mathf.PingPong(secondsShown * settings.BobsPerSecond * 2f, 1f);

            // The arrow stays on the Enemy as far as the screen allows; the text slides sideways to stay whole.
            var half = area.rect.size * 0.5f;
            var margin = settings.ScreenMargin;
            var labelBottom = settings.ArrowSize + labelGap;
            local.x = Mathf.Clamp(local.x, -half.x + margin, half.x - margin);
            local.y = Mathf.Clamp(local.y, -half.y + margin, half.y - margin - labelBottom - labelHeight);
            marker.anchoredPosition = local;

            var mostLeft = -half.x + margin + labelHalfWidth - local.x;
            var mostRight = half.x - margin - labelHalfWidth - local.x;
            label.rectTransform.anchoredPosition = new Vector2(Mathf.Clamp(0f, mostLeft, Mathf.Max(mostLeft, mostRight)), labelBottom);
        }

        private void SetAlpha(float value)
        {
            if (Mathf.Approximately(value, alpha) && group.alpha == value)
            {
                return;
            }

            alpha = value;
            group.alpha = value;
            var visible = value > 0f;
            if (label.enabled != visible)
            {
                label.enabled = visible;
                arrow.enabled = visible;
            }
        }
    }
}
