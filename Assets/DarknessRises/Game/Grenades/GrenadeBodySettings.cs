using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Grenades
{
    /// <summary>How a Grenade looks in flight: a small glowing body with a short trail, so the throw can be followed in Darkness.</summary>
    [Serializable]
    public sealed class GrenadeBodySettings
    {
        [SerializeField, Tooltip("The pooled Grenade in flight.")]
        private AssetReferenceGameObject prefab = new(string.Empty);

        [SerializeField, Tooltip("Colour of the body's glow and its trail. Unlit, so it shows in Darkness without lighting anything.")]
        private Color glowColor = new(1f, 0.45f, 0.15f);

        [SerializeField, Min(0.01f), Tooltip("Metres across the body.")]
        private float size = 0.18f;

        [SerializeField, Min(0f), Tooltip("Seconds the trail lasts. 0 turns the trail off.")]
        private float trailSeconds = 0.2f;

        [SerializeField, Min(0f), Tooltip("Degrees per second the body tumbles in flight.")]
        private float tumbleSpeed = 540f;

        public AssetReferenceGameObject Prefab => prefab;
        public Color GlowColor => glowColor;
        public float Size => size;
        public float TrailSeconds => trailSeconds;
        public float TumbleSpeed => tumbleSpeed;
    }
}
