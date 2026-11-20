using System;
using UnityEngine;

namespace DarknessRises.Game.Enemies.Tells
{
    /// <summary>
    /// How an Enemy type's eyes glint: emissive, so they show in full Darkness, but only near the character, and only
    /// once they've opened after the Enemy spawns.
    /// </summary>
    [Serializable]
    public sealed class EyeGlintSettings
    {
        [SerializeField, Min(0f), Tooltip("Metres from the character within which the eyes glint. About a third of the screen.")]
        private float range = 7f;

        [SerializeField, Min(0f), Tooltip("Metres inside the range over which the glint fades in, so it doesn't pop on.")]
        private float fadeDistance = 1.5f;

        [SerializeField, ColorUsage(false, true), Tooltip("Emission colour of a full glint. HDR: brighter than 1 reads as glowing.")]
        private Color color = new(3f, 0.7f, 0.15f);

        [SerializeField, Tooltip("How the eyes open after the Enemy spawns. The opening multiplies the distance glint.")]
        private EyeOpeningSettings opening = new();

        public float Range => range;
        public float FadeDistance => fadeDistance;
        public Color Color => color;
        public EyeOpeningSettings Opening => opening;
    }
}
