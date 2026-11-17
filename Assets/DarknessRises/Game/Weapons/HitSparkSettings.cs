using System;
using UnityEngine;

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
        private HitSparkView prefab;

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

        public HitSparkView Prefab => prefab;
        public Color Color => color;
        public float Intensity => intensity;
        public float Range => range;
        public float Seconds => seconds;
        public float SurfaceOffset => surfaceOffset;
        public int SparkCount => sparkCount;
    }
}
