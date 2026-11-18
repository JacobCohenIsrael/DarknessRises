using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// The small hit spark where a Shot hits something: a few glowing sparks and a brief, shadowless light
    /// that lights whatever was hit.
    /// </summary>
    [Serializable]
    public sealed class HitSparkSettings
    {
        [SerializeField, Tooltip("The pooled hit spark.")]
        private AssetReferenceGameObject prefab = new(string.Empty);

        [SerializeField] private Color color = new(1f, 0.7f, 0.35f);

        [SerializeField, Min(0f), Tooltip("URP point light intensity at the moment of the hit. It fades to 0 over the duration.")]
        private float intensity = 6f;

        [SerializeField, Min(0f), Tooltip("Metres the light reaches.")]
        private float range = 2.5f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the light lasts.")]
        private float seconds = 0.12f;

        [SerializeField, Min(0f), Tooltip("Metres the light sits off the hit surface, back toward the shooter, so it lights the surface it hit.")]
        private float surfaceOffset = 0.25f;

        [SerializeField, Min(0), Tooltip("Sparks thrown off by one hit.")]
        private int sparkCount = 8;

        [Header("Dull spark")]
        [SerializeField, Tooltip("Colour of the spark when a Shot hits something it can't hurt, such as the Colossus's foot off its weak spot.")]
        private Color dullColor = new(0.55f, 0.55f, 0.6f);

        [SerializeField, Range(0f, 1f), Tooltip("Share of the normal light intensity a dull spark gets.")]
        private float dullIntensityShare = 0.25f;

        [SerializeField, Min(0), Tooltip("Sparks thrown off by a hit that doesn't hurt.")]
        private int dullSparkCount = 3;

        public AssetReferenceGameObject Prefab => prefab;
        public Color Color => color;
        public float Intensity => intensity;
        public float Range => range;
        public float Seconds => seconds;
        public float SurfaceOffset => surfaceOffset;
        public int SparkCount => sparkCount;
        public Color DullColor => dullColor;
        public float DullIntensityShare => dullIntensityShare;
        public int DullSparkCount => dullSparkCount;
    }
}
