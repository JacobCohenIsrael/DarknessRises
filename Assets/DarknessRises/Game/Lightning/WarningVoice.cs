using System;
using System.Collections.Generic;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// The volume of one playing Boss warning (the growl) over time: full until the flash, then fading out. It can be
    /// told to fade out sooner (leaving the Room), but it never stops dead. Plain C# (ADR 0003); <see cref="LightningView"/>
    /// keeps one per warning AudioSource and applies <see cref="Volume"/> to it.
    /// </summary>
    public sealed class WarningVoice
    {
        private float peakVolume;
        private float elapsedSeconds;
        private float fadeStartSeconds;
        private float fadeOutSeconds;

        /// <summary>Whether it's still audible. Once it has faded out it stays silent until the next <see cref="Start"/>.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>The volume right now, from 0 to the volume it started at.</summary>
        public float Volume { get; private set; }

        /// <summary>
        /// Starts at <paramref name="volume"/>, holds it for <paramref name="secondsUntilFade"/> (until the flash), then fades
        /// to silence over <paramref name="fadeSeconds"/>.
        /// </summary>
        public void Start(float volume, float secondsUntilFade, float fadeSeconds)
        {
            if (volume < 0f || secondsUntilFade < 0f || fadeSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(volume), "A warning's volume and times can't be negative.");
            }

            peakVolume = volume;
            elapsedSeconds = 0f;
            fadeStartSeconds = secondsUntilFade;
            fadeOutSeconds = fadeSeconds;
            IsPlaying = true;
            Volume = volume;
        }

        /// <summary>
        /// Fades out from the current volume over <paramref name="seconds"/>, starting now, unless it would already be
        /// silent sooner than that.
        /// </summary>
        public void FadeOut(float seconds)
        {
            if (!IsPlaying)
            {
                return;
            }

            if (seconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A fade can't take negative time.");
            }

            if (SecondsLeft() <= seconds)
            {
                return;
            }

            peakVolume = Volume;
            fadeStartSeconds = elapsedSeconds;
            fadeOutSeconds = seconds;
        }

        /// <summary>Advances <paramref name="deltaSeconds"/> and updates <see cref="Volume"/> and <see cref="IsPlaying"/>.</summary>
        public void Tick(float deltaSeconds)
        {
            if (!IsPlaying)
            {
                return;
            }

            elapsedSeconds += deltaSeconds;
            var fadeSeconds = elapsedSeconds - fadeStartSeconds;
            if (fadeSeconds <= 0f)
            {
                Volume = peakVolume;
                return;
            }

            var remaining = fadeOutSeconds > 0f ? 1f - fadeSeconds / fadeOutSeconds : 0f;
            if (remaining <= 0f)
            {
                Volume = 0f;
                IsPlaying = false;
                return;
            }

            Volume = peakVolume * remaining;
        }

        /// <summary>
        /// Which of <paramref name="voices"/> a new warning should play on: a silent one if there is one, otherwise the
        /// quietest, so a new warning cuts off as little as possible.
        /// </summary>
        public static int PickFor(IReadOnlyList<WarningVoice> voices)
        {
            if (voices == null || voices.Count == 0)
            {
                throw new ArgumentException("There's no warning voice to pick.", nameof(voices));
            }

            var picked = 0;
            for (var i = 0; i < voices.Count; i++)
            {
                if (!voices[i].IsPlaying)
                {
                    return i;
                }

                if (voices[i].Volume < voices[picked].Volume)
                {
                    picked = i;
                }
            }

            return picked;
        }

        private float SecondsLeft()
        {
            return Math.Max(0f, fadeStartSeconds - elapsedSeconds) + fadeOutSeconds -
                   Math.Max(0f, elapsedSeconds - fadeStartSeconds);
        }
    }
}
