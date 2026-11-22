using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// The Muzzle flash: a short burst from a shadowless point light that lights only the area close to the character,
    /// with the Weapon's own look (its prefab): a white flash, a yellow one with sparks, or a glow at a nozzle.
    /// </summary>
    [Serializable]
    public sealed class MuzzleFlashSettings
    {
        /// <summary>The most a flicker takes off the flash, so its light stays on (and Gunfire lights) for its whole duration.</summary>
        public const float MaxFlicker = 0.9f;

        [SerializeField, Tooltip("The pooled Muzzle flash this Weapon shows: its light and what glows at the muzzle.")]
        private AssetReferenceGameObject prefab = new(string.Empty);

        [SerializeField, Tooltip("Colour of the flash's light.")]
        private Color color = new(1f, 0.78f, 0.45f);

        [SerializeField, Min(0f), Tooltip("URP point light intensity at the moment of the shot. It fades to 0 over the duration.")]
        private float intensity = 12f;

        [SerializeField, Min(0f), Tooltip("Metres the light reaches.")]
        private float range = 6f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the flash lasts.")]
        private float seconds = 0.07f;

        [SerializeField, Range(0f, MaxFlicker), Tooltip(
            "How much the flash flickers as it fades: each frame its light and glow drop by up to this share at random. "
            + "0 fades smoothly. Only the look: the light never goes out early, so Gunfire lights just as far.")]
        private float flicker;

        [SerializeField, Min(0), Tooltip("Sparks thrown forward with each flash, if the prefab has sparks. 0 throws none.")]
        private int sparkCount;

        public AssetReferenceGameObject Prefab => prefab;
        public Color Color => color;
        public float Intensity => intensity;
        public float Range => range;
        public float Seconds => seconds;
        public float Flicker => flicker;
        public int SparkCount => sparkCount;

        /// <summary>
        /// How strong the flash is with <paramref name="secondsLeft"/> to go: 1 at the shot, fading to 0, less up to the
        /// flicker share (at most <see cref="MaxFlicker"/>) by <paramref name="random01"/> (0 to 1). Never more than the unflickered fade.
        /// </summary>
        public static float Strength(float secondsLeft, float seconds, float flicker, float random01)
        {
            if (seconds <= 0f || secondsLeft <= 0f)
            {
                return 0f;
            }

            var fade = Mathf.Clamp01(secondsLeft / seconds);
            return fade * (1f - Mathf.Clamp(flicker, 0f, MaxFlicker) * Mathf.Clamp01(random01));
        }
    }
}
