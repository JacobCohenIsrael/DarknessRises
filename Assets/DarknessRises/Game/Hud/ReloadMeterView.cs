using DarknessRises.Game.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// A small radial meter by the character on the HUD that fills over the equipped Primary weapon's reload,
    /// manual or automatic, and is hidden otherwise. It reads the Weapon's own progress, which only moves with
    /// scaled time, so it freezes with the game while paused.
    /// </summary>
    /// <remarks>
    /// Runs late, after the camera has moved this frame, so the meter sits on the character where it's drawn.
    /// Works only while a reload is shown: the events show and hide it, and the per-frame update is a fill amount and a position.
    /// </remarks>
    [DefaultExecutionOrder(AfterCamera)]
    public sealed class ReloadMeterView : MonoBehaviour
    {
        private const int AfterCamera = 10000;

        [SerializeField, Tooltip("Stretched over the HUD canvas; the meter is placed in its space.")]
        private RectTransform area;

        [SerializeField, Tooltip("The meter itself, shown only while reloading. Anchored at the centre of the area.")]
        private RectTransform meter;

        [SerializeField, Tooltip("The camera the character is seen through.")]
        private Camera worldCamera;

        [SerializeField, Tooltip("The faint disc behind the fill.")]
        private Image background;

        [SerializeField, Tooltip("Radial filled image of the reload's progress.")]
        private Image fill;

        private ReloadFeedbackSettings settings;
        private Transform character;
        private PrimaryWeapon weapon;

        /// <summary>Lays the meter out from <paramref name="feedback"/> and follows <paramref name="followed"/> on screen.</summary>
        public void Use(ReloadFeedbackSettings feedback, Transform followed)
        {
            settings = feedback;
            character = followed;
            meter.sizeDelta = new Vector2(settings.MeterSize, settings.MeterSize);
            background.color = settings.BackgroundColor;
            background.enabled = settings.BackgroundColor.a > 0f;
            fill.color = settings.FillColor;
        }

        /// <summary>Shows the reloads of <paramref name="primaryWeapon"/>, the equipped Primary, from now on.</summary>
        public void Show(PrimaryWeapon primaryWeapon)
        {
            Unbind();

            weapon = primaryWeapon;
            weapon.ReloadStarted += Refresh;
            weapon.ReloadFinished += Refresh;

            // A Primary switched to in the middle of its reload shows it at once.
            Refresh();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void LateUpdate()
        {
            if (weapon == null || !weapon.IsReloading)
            {
                return;
            }

            fill.fillAmount = weapon.ReloadProgress;
            Follow();
        }

        private void Unbind()
        {
            if (weapon == null)
            {
                return;
            }

            weapon.ReloadStarted -= Refresh;
            weapon.ReloadFinished -= Refresh;
            weapon = null;
        }

        private void Refresh()
        {
            var reloading = weapon.IsReloading && settings != null;
            meter.gameObject.SetActive(reloading);
            if (reloading)
            {
                fill.fillAmount = weapon.ReloadProgress;
                Follow();
            }
        }

        private void Follow()
        {
            var screenPoint = worldCamera.WorldToScreenPoint(character.position + settings.WorldOffset);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPoint, null, out var localPoint))
            {
                meter.anchoredPosition = localPoint;
            }
        }
    }
}
