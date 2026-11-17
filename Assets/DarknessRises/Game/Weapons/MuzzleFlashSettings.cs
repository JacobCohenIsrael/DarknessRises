using System;
using UnityEngine;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// The Muzzle flash: a short burst from a shadowless point light that lights only the area close to the character.
    /// </summary>
    [Serializable]
    public sealed class MuzzleFlashSettings
    {
        [SerializeField] private Color color = new(1f, 0.78f, 0.45f);

        [SerializeField, Min(0f), Tooltip("URP point light intensity at the moment of the shot. It fades to 0 over the duration.")]
        private float intensity = 12f;

        [SerializeField, Min(0f), Tooltip("Metres the light reaches.")]
        private float range = 6f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the flash lasts.")]
        private float seconds = 0.07f;

        public Color Color => color;
        public float Intensity => intensity;
        public float Range => range;
        public float Seconds => seconds;
    }
}
