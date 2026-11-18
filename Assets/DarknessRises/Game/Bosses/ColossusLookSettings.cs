using System;
using UnityEngine;

namespace DarknessRises.Game.Bosses
{
    /// <summary>
    /// How a Colossus stomp looks and feels: the raised foot's shadow, the fall, the shockwave, the burst of light,
    /// the camera shake and the glowing weak spot.
    /// </summary>
    [Serializable]
    public sealed class ColossusLookSettings
    {
        [Header("Foot")]
        [SerializeField, Min(0f), Tooltip("Metres above the floor the raised foot waits, out of view.")]
        private float raisedHeight = 14f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the foot takes to come down, ending at the impact. Part of the time from flash to impact.")]
        private float fallSeconds = 0.3f;

        [SerializeField, Min(0f), Tooltip("Radius in metres of the shadow the flash shows. It should cover the Position the foot lands on.")]
        private float shadowRadius = 2.2f;

        [SerializeField, Range(0f, 1f), Tooltip("How dark the shadow is against the lit floor.")]
        private float shadowOpacity = 0.85f;

        [Header("Impact")]
        [SerializeField, Min(0f), Tooltip("Metres the shockwave ring spreads to.")]
        private float shockwaveRadius = 6f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the shockwave ring takes to spread and fade.")]
        private float shockwaveSeconds = 0.5f;

        [SerializeField] private Color impactLightColor = new(1f, 0.78f, 0.55f);

        [SerializeField, Min(0f), Tooltip("Intensity of the shadowless burst of light at the impact.")]
        private float impactLightIntensity = 12f;

        [SerializeField, Min(0f)] private float impactLightRange = 12f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the burst of light takes to fade.")]
        private float impactLightSeconds = 0.4f;

        [SerializeField, Min(0f), Tooltip("Strength of the Cinemachine impulse (camera shake) at the impact.")]
        private float impactShakeForce = 1.6f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the camera shake lasts.")]
        private float impactShakeSeconds = 0.45f;

        [Header("Weak spot")]
        [SerializeField, ColorUsage(false, true), Tooltip("Glow of the weak spot while the foot is planted. The only lit part of the foot.")]
        private Color weakSpotGlow = new(2.4f, 0.55f, 0.15f);

        [SerializeField, Min(0f), Tooltip("Intensity of the weak spot's small shadowless light.")]
        private float weakSpotLightIntensity = 3f;

        [SerializeField, Min(0f)] private float weakSpotLightRange = 3.5f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the weak spot flares brighter after a hit.")]
        private float weakSpotHitFlashSeconds = 0.12f;

        [SerializeField, Min(1f), Tooltip("How much brighter the weak spot flares after a hit.")]
        private float weakSpotHitFlashBoost = 2.5f;

        [Header("Sound")]
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        public float RaisedHeight => raisedHeight;
        public float FallSeconds => fallSeconds;
        public float ShadowRadius => shadowRadius;
        public float ShadowOpacity => shadowOpacity;
        public float ShockwaveRadius => shockwaveRadius;
        public float ShockwaveSeconds => shockwaveSeconds;
        public Color ImpactLightColor => impactLightColor;
        public float ImpactLightIntensity => impactLightIntensity;
        public float ImpactLightRange => impactLightRange;
        public float ImpactLightSeconds => impactLightSeconds;
        public float ImpactShakeForce => impactShakeForce;
        public float ImpactShakeSeconds => impactShakeSeconds;
        public Color WeakSpotGlow => weakSpotGlow;
        public float WeakSpotLightIntensity => weakSpotLightIntensity;
        public float WeakSpotLightRange => weakSpotLightRange;
        public float WeakSpotHitFlashSeconds => weakSpotHitFlashSeconds;
        public float WeakSpotHitFlashBoost => weakSpotHitFlashBoost;
        public float Volume => volume;
    }
}
