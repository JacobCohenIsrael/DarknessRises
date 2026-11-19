using System;

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// How long the sounds a pooled object started still need it: the clip's length left after where it started,
    /// counted in clip seconds, so a lower pitch makes it last longer. Plain C# (ADR 0003). A pooled view keeps one,
    /// tells it about each sound it plays and goes back to its pool only once its visuals are done and
    /// <see cref="IsPlaying"/> is false, so switching it off never cuts a sound short.
    /// </summary>
    public sealed class SoundTail
    {
        private float clipSecondsLeft;

        /// <summary>Whether a sound it was told about is still playing.</summary>
        public bool IsPlaying => clipSecondsLeft > 0f;

        /// <summary>Clip seconds left of the longest sound still playing.</summary>
        public float ClipSecondsLeft => clipSecondsLeft;

        /// <summary>
        /// Real seconds a clip of <paramref name="clipLengthSeconds"/>, started <paramref name="startSeconds"/> in,
        /// plays for at <paramref name="pitch"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">A negative length or start, or a pitch that isn't positive.</exception>
        public static float Seconds(float clipLengthSeconds, float startSeconds, float pitch)
        {
            if (pitch <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(pitch), pitch, "Sounds only play forwards, so pitch must be positive.");
            }

            return Remaining(clipLengthSeconds, startSeconds) / pitch;
        }

        /// <summary>
        /// A sound started: a clip of <paramref name="clipLengthSeconds"/>, from <paramref name="startSeconds"/> in.
        /// Sounds overlap, so it lasts as long as the longest of them.
        /// </summary>
        public void Play(float clipLengthSeconds, float startSeconds = 0f)
        {
            var remaining = Remaining(clipLengthSeconds, startSeconds);
            if (remaining > clipSecondsLeft)
            {
                clipSecondsLeft = remaining;
            }
        }

        /// <summary>Advances by <paramref name="deltaSeconds"/> of real time, played at <paramref name="pitch"/>.</summary>
        public void Tick(float deltaSeconds, float pitch)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time only moves forward.");
            }

            if (pitch < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(pitch), pitch, "Sounds only play forwards.");
            }

            clipSecondsLeft -= deltaSeconds * pitch;
            if (clipSecondsLeft < 0f)
            {
                clipSecondsLeft = 0f;
            }
        }

        /// <summary>Forgets every sound, for an object that stopped them itself.</summary>
        public void Clear()
        {
            clipSecondsLeft = 0f;
        }

        private static float Remaining(float clipLengthSeconds, float startSeconds)
        {
            if (clipLengthSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(clipLengthSeconds), clipLengthSeconds, "A clip can't be shorter than nothing.");
            }

            if (startSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(startSeconds), startSeconds, "A clip can't start before its beginning.");
            }

            return clipLengthSeconds > startSeconds ? clipLengthSeconds - startSeconds : 0f;
        }
    }
}
