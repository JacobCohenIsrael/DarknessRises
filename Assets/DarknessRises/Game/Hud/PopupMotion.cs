using UnityEngine;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// How a short-lived mark on the HUD (points over a kill, a hit marker) moves through its life: where it is, how
    /// big, and how much it shows, as plain functions of how far through its life it is.
    /// </summary>
    public static class PopupMotion
    {
        // The last share of its life over which a popup fades out.
        private const float FadeShare = 0.4f;

        /// <summary>How far through its life a mark is, from 0 to 1, <paramref name="elapsedSeconds"/> into <paramref name="seconds"/>.</summary>
        public static float Progress(float elapsedSeconds, float seconds)
        {
            return seconds > 0f ? Mathf.Clamp01(elapsedSeconds / seconds) : 1f;
        }

        /// <summary>How far points have floated up: fast at first, slowing as they go, <paramref name="distance"/> by the end.</summary>
        public static float Rise(float progress, float distance)
        {
            var left = 1f - Mathf.Clamp01(progress);
            return distance * (1f - left * left);
        }

        /// <summary>How much points show: fully until the last part of their life, then fading to nothing.</summary>
        public static float PointsAlpha(float progress)
        {
            return Mathf.Clamp01((1f - Mathf.Clamp01(progress)) / FadeShare);
        }

        /// <summary>The size of a hit marker: from <paramref name="startScale"/> times its size down to its size.</summary>
        public static float MarkerScale(float progress, float startScale)
        {
            return Mathf.Lerp(startScale, 1f, Mathf.Clamp01(progress));
        }

        /// <summary>How much a hit marker shows: it fades over its whole short life.</summary>
        public static float MarkerAlpha(float progress)
        {
            return 1f - Mathf.Clamp01(progress);
        }
    }
}
