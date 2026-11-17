using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// How a Weapon's Shots look: an unlit, glowing body with a short trail, so every Shot can be followed in Darkness.
    /// </summary>
    [Serializable]
    public sealed class ShotSettings
    {
        [SerializeField, Tooltip("The pooled Shot to fire.")]
        private AssetReferenceGameObject prefab = new(string.Empty);

        [SerializeField, ColorUsage(false, true), Tooltip("Glow colour of the Shot and its trail. Unlit, so it shows without any light.")]
        private Color glowColor = new(1f, 0.75f, 0.35f);

        [SerializeField, Min(0.01f), Tooltip("Diameter of the Shot's glowing body, in metres.")]
        private float size = 0.12f;

        [SerializeField, Min(0f), Tooltip("Seconds the trail behind a Shot lasts. 0 hides the trail.")]
        private float trailSeconds = 0.06f;

        [SerializeField, Min(0.01f), Tooltip("How many times its starting size the Shot has grown by the end of its range, like a spreading flame. 1 keeps its size.")]
        private float sizeAtRange = 1f;

        public AssetReferenceGameObject Prefab => prefab;
        public Color GlowColor => glowColor;
        public float Size => size;
        public float TrailSeconds => trailSeconds;
        public float SizeAtRange => sizeAtRange;
    }
}
