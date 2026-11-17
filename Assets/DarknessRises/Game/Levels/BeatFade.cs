using UnityEngine;

namespace DarknessRises.Game.Levels
{
    /// <summary>How strongly something shows over a beat: it fades in, holds, then fades out.</summary>
    public static class BeatFade
    {
        /// <summary>From 0 (gone) to 1 (fully shown), <paramref name="elapsedSeconds"/> into the beat.</summary>
        public static float InHoldOut(float elapsedSeconds, float fadeInSeconds, float holdSeconds, float fadeOutSeconds)
        {
            if (elapsedSeconds < 0f)
            {
                return 0f;
            }

            if (elapsedSeconds < fadeInSeconds)
            {
                return elapsedSeconds / fadeInSeconds;
            }

            var afterHold = elapsedSeconds - fadeInSeconds - holdSeconds;
            if (afterHold <= 0f)
            {
                return 1f;
            }

            return fadeOutSeconds > 0f ? Mathf.Clamp01(1f - afterHold / fadeOutSeconds) : 0f;
        }

        /// <summary>Goes from 0 to 1 over <paramref name="seconds"/>, then stays at 1.</summary>
        public static float Rise(float elapsedSeconds, float seconds)
        {
            return seconds > 0f ? Mathf.Clamp01(elapsedSeconds / seconds) : 1f;
        }
    }
}
