using System;
using UnityEngine;

namespace DarknessRises.Game.Levels
{
    /// <summary>
    /// The Level's start beat: a short title card while the character walks in through the door
    /// and the light behind it fades to Darkness.
    /// </summary>
    [Serializable]
    public sealed class LevelStartSettings
    {
        [SerializeField, Min(0f), Tooltip("Seconds the start beat lasts. Stragglers only come once it's over.")]
        private float durationSeconds = 4f;

        [Header("Title card")]
        [SerializeField, Min(0f)] private float titleFadeInSeconds = 0.6f;
        [SerializeField, Min(0f)] private float titleHoldSeconds = 2f;
        [SerializeField, Min(0f)] private float titleFadeOutSeconds = 0.9f;

        [Header("Light behind the door")]
        [SerializeField, Tooltip("Shadowless light outside the entrance, behind the character as it walks in.")]
        private Color doorLightColor = new(1f, 0.78f, 0.5f);

        [SerializeField, Min(0f), Tooltip("Intensity at the start of the beat. It fades to Darkness over the whole beat.")]
        private float doorLightIntensity = 6f;

        [SerializeField, Min(0f), Tooltip("Metres the light reaches.")]
        private float doorLightRange = 12f;

        public float DurationSeconds => durationSeconds;
        public Color DoorLightColor => doorLightColor;
        public float DoorLightRange => doorLightRange;

        /// <summary>How much the title card shows, from 0 to 1, <paramref name="elapsedSeconds"/> into the beat.</summary>
        public float TitleAlpha(float elapsedSeconds)
        {
            return BeatFade.InHoldOut(elapsedSeconds, titleFadeInSeconds, titleHoldSeconds, titleFadeOutSeconds);
        }

        /// <summary>Intensity of the light behind the door, <paramref name="elapsedSeconds"/> into the beat. It eases out to 0.</summary>
        public float DoorLightIntensityAt(float elapsedSeconds)
        {
            var left = 1f - BeatFade.Rise(elapsedSeconds, durationSeconds);
            return doorLightIntensity * left * left;
        }
    }
}
