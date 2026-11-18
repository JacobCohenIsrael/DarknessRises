using System;
using UnityEngine;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// How one Lightning flash looks: an electric-blue key light through the windows that throws long, sharp shadows,
    /// a shadowless fill so the whole Room shows, and the windows themselves lighting up.
    /// </summary>
    [Serializable]
    public sealed class LightningFlashSettings
    {
        [SerializeField, Min(0.05f), Tooltip("Seconds from the strike until the Room is back to Darkness.")]
        private float durationSeconds = 1.1f;

        [SerializeField, Tooltip("Brightness over the flash, from 0 (dark) to 1 (peak), over normalized time 0 to 1. " +
                                 "The default strikes, flickers once and fades.")]
        private AnimationCurve shape = DefaultShape();

        [SerializeField, Range(5f, 85f), Tooltip("Degrees above the horizon the Lightning comes in at. Lower throws longer shadows.")]
        private float elevationDegrees = 28f;

        [Header("Key light (the only shadow-casting light)")]
        [SerializeField] private Color keyColor = new(0.45f, 0.65f, 1f);

        [SerializeField, Min(0f), Tooltip("URP directional light intensity at the peak of the flash.")]
        private float keyIntensity = 4.5f;

        [SerializeField, Range(0f, 1f), Tooltip("How dark the shadows are. 1 is fully black.")]
        private float shadowStrength = 1f;

        [Header("Fill light (shadowless)")]
        [SerializeField] private Color fillColor = new(0.25f, 0.4f, 1f);

        [SerializeField, Min(0f), Tooltip("URP directional light intensity at the peak, lighting the whole Room.")]
        private float fillIntensity = 0.6f;

        [SerializeField, Range(5f, 90f), Tooltip("Degrees above the horizon the fill comes from.")]
        private float fillElevationDegrees = 45f;

        [SerializeField, Range(-180f, 180f), Tooltip("Degrees the fill turns from the key light's direction. " +
                                                     "180 is light bouncing back off the far walls, so the faces in shadow aren't pure black.")]
        private float fillYawOffsetDegrees = 180f;

        [Header("Windows")]
        [SerializeField, ColorUsage(false, true), Tooltip("The sky seen through the windows at the peak of the flash.")]
        private Color windowGlowColor = new(0.6f, 1f, 2.6f);

        public float DurationSeconds => durationSeconds;
        public float ElevationDegrees => elevationDegrees;
        public Color KeyColor => keyColor;
        public float KeyIntensity => keyIntensity;
        public float ShadowStrength => shadowStrength;
        public Color FillColor => fillColor;
        public float FillIntensity => fillIntensity;
        public float FillElevationDegrees => fillElevationDegrees;
        public float FillYawOffsetDegrees => fillYawOffsetDegrees;
        public Color WindowGlowColor => windowGlowColor;

        /// <summary>Brightness from 0 to 1, <paramref name="elapsedSeconds"/> after the strike.</summary>
        public float Brightness(float elapsedSeconds)
        {
            if (elapsedSeconds < 0f || elapsedSeconds >= durationSeconds)
            {
                return 0f;
            }

            return Mathf.Clamp01(shape.Evaluate(elapsedSeconds / durationSeconds));
        }

        private static AnimationCurve DefaultShape()
        {
            // Strike, a quick dip, a second flicker, then a long fade back to Darkness.
            return Linear(
                new Vector2(0f, 0f),
                new Vector2(0.02f, 1f),
                new Vector2(0.07f, 0.12f),
                new Vector2(0.12f, 0.85f),
                new Vector2(0.2f, 0.5f),
                new Vector2(0.4f, 0.22f),
                new Vector2(0.7f, 0.06f),
                new Vector2(1f, 0f));
        }

        private static AnimationCurve Linear(params Vector2[] points)
        {
            var keys = new Keyframe[points.Length];
            for (var i = 0; i < points.Length; i++)
            {
                var inTangent = i > 0 ? Slope(points[i - 1], points[i]) : 0f;
                var outTangent = i < points.Length - 1 ? Slope(points[i], points[i + 1]) : 0f;
                keys[i] = new Keyframe(points[i].x, points[i].y, inTangent, outTangent);
            }

            return new AnimationCurve(keys);
        }

        private static float Slope(Vector2 from, Vector2 to)
        {
            return (to.y - from.y) / (to.x - from.x);
        }
    }
}
