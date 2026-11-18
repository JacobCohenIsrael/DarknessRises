using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Grenades
{
    /// <summary>
    /// The Grenade's explosion: a burst of fire and sparks, a brief shadowless light that lights the area up,
    /// a bang, and a camera shake.
    /// </summary>
    [Serializable]
    public sealed class GrenadeExplosionSettings
    {
        [SerializeField, Tooltip("The pooled explosion.")]
        private AssetReferenceGameObject prefab = new(string.Empty);

        [Header("Light")]
        [SerializeField] private Color lightColor = new(1f, 0.62f, 0.3f);

        [SerializeField, Min(0f), Tooltip("URP point light intensity at the peak of the flash.")]
        private float lightIntensity = 80f;

        [SerializeField, Min(0f), Tooltip("Metres the light reaches. Wider than the blast, so the player sees what's around it.")]
        private float lightRange = 9f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the light lasts, fading out from its peak.")]
        private float lightSeconds = 0.9f;

        [SerializeField, Min(0f), Tooltip("Metres above the floor the light sits, so it lights the floor and the Enemies standing on it.")]
        private float lightHeight = 1.2f;

        [Header("Burst")]
        [SerializeField, Min(0), Tooltip("Fireball puffs thrown out by one explosion.")]
        private int fireballCount = 18;

        [SerializeField, Min(0), Tooltip("Sparks thrown out by one explosion.")]
        private int sparkCount = 40;

        [Header("Sound")]
        [SerializeField] private AssetReferenceT<AudioClip> clip = new(string.Empty);

        [SerializeField, Min(0f), Tooltip("Seconds into the clip the bang starts, so its blast lands with the explosion " +
                                          "instead of a build-up before it. Set from the clip's waveform: Grenade3 builds up " +
                                          "for about half a second before it hits.")]
        private float clipStartSeconds = 0.45f;

        [SerializeField, Range(0f, 1f)] private float volume = 0.9f;

        [SerializeField, Min(1f), Tooltip("Metres the bang carries before it fades to nothing.")]
        private float hearingRange = 60f;

        [Header("Camera shake")]
        [SerializeField, Min(0f), Tooltip("Strength of the Cinemachine impulse. 1 is a normal bump.")]
        private float shakeForce = 0.6f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the shake lasts.")]
        private float shakeSeconds = 0.45f;

        public AssetReferenceGameObject Prefab => prefab;
        public Color LightColor => lightColor;
        public float LightIntensity => lightIntensity;
        public float LightRange => lightRange;
        public float LightSeconds => lightSeconds;
        public float LightHeight => lightHeight;
        public int FireballCount => fireballCount;
        public int SparkCount => sparkCount;
        public AssetReferenceT<AudioClip> Clip => clip;
        public float ClipStartSeconds => clipStartSeconds;
        public float Volume => volume;
        public float HearingRange => hearingRange;
        public float ShakeForce => shakeForce;
        public float ShakeSeconds => shakeSeconds;
    }
}
