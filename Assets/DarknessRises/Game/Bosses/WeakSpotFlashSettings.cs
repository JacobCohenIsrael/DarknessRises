using System;
using UnityEngine;

namespace DarknessRises.Game.Bosses
{
    /// <summary>
    /// The weak spot flaring when a Shot hurts it: its glow and light jump brighter, then fade back over the flash time.
    /// A Shot that doesn't hurt (off the weak spot, or outside the damage window) never flashes it.
    /// </summary>
    [Serializable]
    public sealed class WeakSpotFlashSettings
    {
        [SerializeField, Min(0.01f), Tooltip("Seconds the flash takes to fade back to the weak spot's normal glow.")]
        private float seconds = 0.2f;

        [SerializeField, Min(1f), Tooltip("How many times brighter the glow and its light are at the moment of the hit.")]
        private float brightness = 4f;

        public float Seconds => seconds;
        public float Brightness => brightness;

        /// <summary>The glow's multiplier with <paramref name="secondsLeft"/> of the flash to go: from the full brightness down to 1.</summary>
        public float BrightnessAt(float secondsLeft)
        {
            var left = Mathf.Clamp01(secondsLeft / seconds);
            return Mathf.Lerp(1f, brightness, left * left);
        }
    }
}
