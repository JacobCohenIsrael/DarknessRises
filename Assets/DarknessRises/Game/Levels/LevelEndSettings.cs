using System;
using UnityEngine;

namespace DarknessRises.Game.Levels
{
    /// <summary>
    /// The Level's end beat: once the last fight is over, a short, calm, dimly lit moment by the far door,
    /// then the character walks out as the screen fades to black.
    /// </summary>
    [Serializable]
    public sealed class LevelEndSettings
    {
        [SerializeField, Min(0f), Tooltip("Seconds the character stands still in the calm before walking out.")]
        private float calmSeconds = 3f;

        [SerializeField, Min(0f), Tooltip("Seconds the screen takes to fade to black as the character walks out.")]
        private float fadeOutSeconds = 1.5f;

        [SerializeField, Min(0f), Tooltip("Seconds every sound of the Level (the Boss's defeat, Enemy footsteps, the character) takes to fade to silence once the character walks out, so nothing is loud over the black screen and nothing is still playing to stop dead when the Level unloads. At most the screen's fade, so it's silent by black.")]
        private float soundFadeOutSeconds = 1.5f;

        [Header("Calm light by the far door")]
        [SerializeField, Tooltip("Shadowless light by the exit. It rises during the calm.")]
        private Color calmLightColor = new(1f, 0.82f, 0.6f);

        [SerializeField, Min(0f), Tooltip("Intensity once it has risen. Dim: a breather, not daylight.")]
        private float calmLightIntensity = 2f;

        [SerializeField, Min(0f), Tooltip("Metres the light reaches.")]
        private float calmLightRange = 9f;

        [SerializeField, Min(0f), Tooltip("Seconds the calm light takes to rise.")]
        private float calmLightRiseSeconds = 1.5f;

        [Header("Card")]
        [SerializeField, Min(0f)] private float cardFadeInSeconds = 0.6f;

        [SerializeField, Min(0f), Tooltip("Seconds the closing card stays on the black screen before the next Level starts.")]
        private float cardHoldSeconds = 1.2f;

        public float CalmSeconds => calmSeconds;
        public float CardHoldSeconds => cardHoldSeconds;
        public float FadeOutSeconds => fadeOutSeconds;

        /// <summary>Seconds the Level's own sounds take to fade to silence once the character walks out.</summary>
        public float SoundFadeOutSeconds => soundFadeOutSeconds;
        public Color CalmLightColor => calmLightColor;
        public float CalmLightRange => calmLightRange;

        /// <summary>Intensity of the calm light, <paramref name="elapsedSeconds"/> after the calm begins.</summary>
        public float CalmLightIntensityAt(float elapsedSeconds)
        {
            return calmLightIntensity * BeatFade.Rise(elapsedSeconds, calmLightRiseSeconds);
        }

        /// <summary>How black the screen is, from 0 to 1, <paramref name="elapsedSeconds"/> after the character starts walking out.</summary>
        public float ScreenFadeAt(float elapsedSeconds)
        {
            return BeatFade.Rise(elapsedSeconds, fadeOutSeconds);
        }

        /// <summary>How much the closing card shows, <paramref name="elapsedSeconds"/> after the screen went black.</summary>
        public float CardAlphaAt(float elapsedSeconds)
        {
            return BeatFade.Rise(elapsedSeconds, cardFadeInSeconds);
        }
    }
}
