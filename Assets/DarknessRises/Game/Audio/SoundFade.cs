using System;

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// The volume of one playing sound that can be cut short without stopping dead: it holds its volume until told to
    /// fade out, then falls to silence over the fade time. Plain C# (ADR 0003); a view applies <see cref="Volume"/> to its
    /// AudioSource each frame and stops the source once <see cref="IsAudible"/> goes false.
    /// </summary>
    public sealed class SoundFade
    {
        private float fadeFromVolume;
        private float fadeSeconds;
        private float fadedSeconds;

        /// <summary>Whether it's playing and hasn't faded to silence yet.</summary>
        public bool IsAudible { get; private set; }

        /// <summary>Whether it's fading out.</summary>
        public bool IsFading { get; private set; }

        /// <summary>The volume right now.</summary>
        public float Volume { get; private set; }

        /// <summary>Starts playing at <paramref name="volume"/>, which it holds until <see cref="FadeOut"/>.</summary>
        public void Start(float volume)
        {
            if (volume < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(volume), volume, "A sound's volume can't be negative.");
            }

            Volume = volume;
            IsAudible = true;
            IsFading = false;
        }

        /// <summary>
        /// Fades out from the current volume to silence over <paramref name="seconds"/>, starting now. A fade already
        /// under way carries on as it is. 0 silences it at once.
        /// </summary>
        public void FadeOut(float seconds)
        {
            if (seconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A fade can't take negative time.");
            }

            if (!IsAudible || IsFading)
            {
                return;
            }

            IsFading = true;
            fadeFromVolume = Volume;
            fadeSeconds = seconds;
            fadedSeconds = 0f;
            if (seconds <= 0f)
            {
                Silence();
            }
        }

        /// <summary>Silent at once, for a sound that ended on its own.</summary>
        public void Stop()
        {
            Silence();
        }

        /// <summary>Advances a fade by <paramref name="deltaSeconds"/>. Does nothing to a sound that isn't fading.</summary>
        public void Tick(float deltaSeconds)
        {
            if (!IsFading)
            {
                return;
            }

            fadedSeconds += deltaSeconds;
            var remaining = 1f - fadedSeconds / fadeSeconds;
            if (remaining <= 0f)
            {
                Silence();
                return;
            }

            Volume = fadeFromVolume * remaining;
        }

        private void Silence()
        {
            Volume = 0f;
            IsAudible = false;
            IsFading = false;
        }
    }
}
