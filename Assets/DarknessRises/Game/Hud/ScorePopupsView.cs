using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// The short-lived marks of the arcade look: the points that pop up over a kill and float away, and the hit marker
    /// that flashes where a Shot hurts an Enemy. Both come from small fixed pools built into the HUD and are reused,
    /// the oldest first when all are busy. Points are written with TMP's number formatting, so nothing is allocated.
    /// <see cref="PopupMotion"/> says how each mark moves; this follows its place in the Room on screen.
    /// </summary>
    public sealed class ScorePopupsView : MonoBehaviour
    {
        private const string PointsFormat = "+{0}";

        [SerializeField, Tooltip("The HUD area the marks move in: stretched over the whole canvas.")]
        private RectTransform area;

        [SerializeField, Tooltip("The pooled points labels.")]
        private TMP_Text[] points = Array.Empty<TMP_Text>();

        [SerializeField, Tooltip("The pooled hit markers.")]
        private Image[] markers = Array.Empty<Image>();

        [SerializeField, Tooltip("The camera the player sees the Room through.")]
        private Camera worldCamera;

        private ScoreHudSettings settings;
        private LiveMark[] livePoints;
        private LiveMark[] liveMarkers;
        private int nextPoints;
        private int nextMarker;

        /// <summary>Gets the pools ready to show marks as <paramref name="hudSettings"/> say.</summary>
        public void Use(ScoreHudSettings hudSettings)
        {
            settings = hudSettings ?? throw new ArgumentNullException(nameof(hudSettings));
            livePoints = new LiveMark[points.Length];
            liveMarkers = new LiveMark[markers.Length];
            for (var i = 0; i < points.Length; i++)
            {
                points[i].fontSize = settings.PopupFontSize;
                points[i].enabled = false;
            }

            for (var i = 0; i < markers.Length; i++)
            {
                markers[i].rectTransform.sizeDelta = new Vector2(settings.HitMarkerSize, settings.HitMarkerSize);
                markers[i].enabled = false;
            }
        }

        /// <summary>
        /// Pops <paramref name="earned"/> points over the Enemy that died at <paramref name="enemyPosition"/>, in the
        /// Combo's colour when the Combo <paramref name="multiplied"/> them.
        /// </summary>
        public void Pop(Vector3 enemyPosition, int earned, bool multiplied)
        {
            if (settings == null || points.Length == 0 || earned <= 0)
            {
                return;
            }

            var index = nextPoints;
            nextPoints = (nextPoints + 1) % points.Length;
            var label = points[index];
            label.SetText(PointsFormat, earned);
            label.color = multiplied ? settings.PopupComboColor : settings.PopupColor;
            label.enabled = true;
            livePoints[index] = new LiveMark(enemyPosition + settings.PopupWorldOffset);
            Place(label.rectTransform, livePoints[index].World, 0f);
        }

        /// <summary>Flashes a hit marker where a Shot hurt an Enemy, at <paramref name="hitPoint"/>.</summary>
        public void Mark(Vector3 hitPoint)
        {
            if (settings == null || markers.Length == 0)
            {
                return;
            }

            var index = nextMarker;
            nextMarker = (nextMarker + 1) % markers.Length;
            var marker = markers[index];
            marker.color = settings.HitMarkerColor;
            marker.enabled = true;
            liveMarkers[index] = new LiveMark(hitPoint);
            marker.rectTransform.localScale = Vector3.one * settings.HitMarkerStartScale;
            Place(marker.rectTransform, hitPoint, 0f);
        }

        private void Update()
        {
            if (settings == null)
            {
                return;
            }

            var deltaSeconds = Time.deltaTime;
            for (var i = 0; i < livePoints.Length; i++)
            {
                if (!livePoints[i].IsLive)
                {
                    continue;
                }

                livePoints[i].Elapsed += deltaSeconds;
                var progress = PopupMotion.Progress(livePoints[i].Elapsed, settings.PopupSeconds);
                var label = points[i];
                if (progress >= 1f)
                {
                    livePoints[i].IsLive = false;
                    label.enabled = false;
                    continue;
                }

                label.alpha = PopupMotion.PointsAlpha(progress);
                Place(label.rectTransform, livePoints[i].World, PopupMotion.Rise(progress, settings.PopupRise));
            }

            for (var i = 0; i < liveMarkers.Length; i++)
            {
                if (!liveMarkers[i].IsLive)
                {
                    continue;
                }

                liveMarkers[i].Elapsed += deltaSeconds;
                var progress = PopupMotion.Progress(liveMarkers[i].Elapsed, settings.HitMarkerSeconds);
                var marker = markers[i];
                if (progress >= 1f)
                {
                    liveMarkers[i].IsLive = false;
                    marker.enabled = false;
                    continue;
                }

                var tint = settings.HitMarkerColor;
                tint.a *= PopupMotion.MarkerAlpha(progress);
                marker.color = tint;
                var scale = PopupMotion.MarkerScale(progress, settings.HitMarkerStartScale);
                marker.rectTransform.localScale = new Vector3(scale, scale, 1f);
                Place(marker.rectTransform, liveMarkers[i].World, 0f);
            }
        }

        // The HUD is a Screen Space Overlay canvas, so a screen point maps to it with no camera.
        private void Place(RectTransform mark, Vector3 world, float rise)
        {
            var screen = worldCamera.WorldToScreenPoint(world);
            if (screen.z > 0f && RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, null, out var local))
            {
                local.y += rise;
                mark.anchoredPosition = local;
            }
        }

        // One mark on screen: where in the Room it's pinned and how long it has been up.
        private struct LiveMark
        {
            public LiveMark(Vector3 world)
            {
                World = world;
                Elapsed = 0f;
                IsLive = true;
            }

            public Vector3 World;
            public float Elapsed;
            public bool IsLive;
        }
    }
}
