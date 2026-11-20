using System;
using UnityEngine;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// How one Lightning flash looks: an electric-blue key light through the windows that throws long, sharp shadows
    /// from a different angle each strike (now and then a low one), a shadowless fill so the whole Room shows, and the windows
    /// themselves lighting up.
    /// </summary>
    [Serializable]
    public sealed class LightningFlashSettings
    {
        [SerializeField, Min(0.05f), Tooltip("Seconds from the strike until the Room is back to Darkness.")]
        private float durationSeconds = 0.42f;

        [SerializeField, Tooltip("Brightness over the flash, from 0 (dark) to 1 (peak), over normalized time 0 to 1. " +
                                 "The default is a single smooth spike that peaks 40% of the way through.")]
        private AnimationCurve shape = DefaultShape();

        [Header("Angle (varies with every flash)")]
        [SerializeField, Range(5f, 85f), Tooltip("Lowest degrees above the horizon a strike comes in at. Lower throws longer shadows.")]
        private float minElevationDegrees = 18f;

        [SerializeField, Range(5f, 85f), Tooltip("Highest degrees above the horizon a strike comes in at.")]
        private float maxElevationDegrees = 46f;

        [SerializeField, Range(0f, 90f), Tooltip("Degrees a strike's direction may turn either way from the Room's Lightning direction.")]
        private float yawJitterDegrees = 28f;

        [SerializeField, Range(0f, 1f), Tooltip("Chance, from 0 to 1, that a strike comes in low instead, from the low range below, " +
                                                "throwing long shadows across the floor and up the walls.")]
        private float lowChance = 0.25f;

        [SerializeField, Range(5f, 85f), Tooltip("Lowest degrees above the horizon a low strike comes in at.")]
        private float minLowElevationDegrees = 8f;

        [SerializeField, Range(5f, 85f), Tooltip("Highest degrees above the horizon a low strike comes in at.")]
        private float maxLowElevationDegrees = 14f;

        [SerializeField, Min(1f), Tooltip("Most times brighter the key light shines for a strike lower than the middle of the ordinary range. " +
                                          "Light from low spreads over more floor, so a low or looming strike shines brighter to reveal the Room as well as an ordinary one.")]
        private float maxLowBrightnessBoost = 3f;

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
        public LightningAngles Angles => new(
            minElevationDegrees,
            maxElevationDegrees,
            yawJitterDegrees,
            lowChance,
            minLowElevationDegrees,
            maxLowElevationDegrees);

        public float MaxLowBrightnessBoost => maxLowBrightnessBoost;
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

        /// <summary>Keeps the elevation range the right way round while it's edited.</summary>
        public void Validate()
        {
            maxElevationDegrees = Mathf.Max(maxElevationDegrees, minElevationDegrees);
            maxLowElevationDegrees = Mathf.Max(maxLowElevationDegrees, minLowElevationDegrees);
        }

        private static AnimationCurve DefaultShape()
        {
            // One smooth spike, like the 2019 strike: up to the peak 40% of the way through, eased back to Darkness.
            // Default curve shape only; the authored curve on the asset is what plays.
            const float peakTime = 0.4f;
            const float riseSlope = 1f / peakTime;
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0f, riseSlope),
                new Keyframe(peakTime, 1f, 0f, 0f),
                new Keyframe(1f, 0f, 0f, 0f));
        }
    }
}
