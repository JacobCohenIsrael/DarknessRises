using System;
using UnityEngine;

namespace DarknessRises.Game.Player
{
    /// <summary>
    /// How the Aura looks: a faint point light that shows the character's own position and little else.
    /// </summary>
    [Serializable]
    public sealed class AuraSettings
    {
        [SerializeField] private Color color = new(1f, 0.82f, 0.6f);

        [SerializeField, Min(0f), Tooltip("URP point light intensity.")]
        private float intensity = 2f;

        [SerializeField, Min(0f), Tooltip("Metres the light reaches.")]
        private float range = 4.5f;

        [SerializeField, Min(0f), Tooltip("Metres above the character's feet.")]
        private float height = 2.6f;

        public Color Color => color;
        public float Intensity => intensity;
        public float Range => range;
        public float Height => height;
    }
}
