using System;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// Lines the reload sound, one clip of a whole reload sequence, up with a reload so the clip ends as the reload
    /// finishes, whatever the reload time. A reload shorter than the clip starts it partway in; a longer one waits
    /// before starting it from the top. Plain C# (ADR 0003).
    /// </summary>
    /// <remarks>
    /// Times are on the reload's own clock (seconds since it started), so whatever freezes the reload (the pause,
    /// a death) holds the sound's place too. At a pitch other than 1 the clip plays faster or slower, and the
    /// alignment allows for it.
    /// </remarks>
    public readonly struct ReloadSoundAlignment
    {
        private readonly float pitch;

        private ReloadSoundAlignment(float clipStartSeconds, float delaySeconds, float playSeconds, float pitch)
        {
            ClipStartSeconds = clipStartSeconds;
            DelaySeconds = delaySeconds;
            PlaySeconds = playSeconds;
            this.pitch = pitch;
        }

        /// <summary>Where in the clip playback starts, in clip seconds. 0 unless the reload is shorter than the clip.</summary>
        public float ClipStartSeconds { get; }

        /// <summary>Reload seconds before the clip starts. 0 unless the reload is longer than the clip.</summary>
        public float DelaySeconds { get; }

        /// <summary>Reload seconds the clip plays for, from its start to its end.</summary>
        public float PlaySeconds { get; }

        /// <summary>Reload seconds at which the clip ends: the reload time, unless the reload has no time at all.</summary>
        public float EndSeconds => DelaySeconds + PlaySeconds;

        /// <summary>
        /// The alignment of a clip <paramref name="clipSeconds"/> long, played at <paramref name="pitch"/>, with a reload
        /// of <paramref name="reloadSeconds"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">A negative clip length or reload time, or a pitch that isn't positive.</exception>
        public static ReloadSoundAlignment For(float clipSeconds, float reloadSeconds, float pitch)
        {
            if (clipSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(clipSeconds), clipSeconds, "A clip can't be shorter than nothing.");
            }

            if (reloadSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(reloadSeconds), reloadSeconds, "A reload can't take negative time.");
            }

            if (pitch <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(pitch), pitch, "The reload sound plays forwards, at a positive pitch.");
            }

            // How long the whole clip takes to play at this pitch, on the reload's clock.
            var clipPlaySeconds = clipSeconds / pitch;
            if (reloadSeconds < clipPlaySeconds)
            {
                // Skip the part of the clip that doesn't fit, so its end still lands on the reload's.
                return new ReloadSoundAlignment((clipPlaySeconds - reloadSeconds) * pitch, 0f, reloadSeconds, pitch);
            }

            return new ReloadSoundAlignment(0f, reloadSeconds - clipPlaySeconds, clipPlaySeconds, pitch);
        }

        /// <summary>
        /// Where in the clip the sound is <paramref name="reloadElapsedSeconds"/> into the reload, in clip seconds.
        /// </summary>
        /// <returns>False before the clip starts and once it has ended.</returns>
        public bool TryClipTimeAt(float reloadElapsedSeconds, out float clipTimeSeconds)
        {
            var playedSeconds = reloadElapsedSeconds - DelaySeconds;
            if (playedSeconds < 0f || playedSeconds >= PlaySeconds)
            {
                clipTimeSeconds = 0f;
                return false;
            }

            clipTimeSeconds = ClipStartSeconds + playedSeconds * pitch;
            return true;
        }
    }
}
