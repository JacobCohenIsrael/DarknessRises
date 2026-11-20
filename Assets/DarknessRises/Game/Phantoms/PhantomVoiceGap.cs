using System;

namespace DarknessRises.Game.Phantoms
{
    /// <summary>
    /// The fewest seconds between two Phantom voices starting, so whispers, breaths and hushes don't pile up on each
    /// other as eyes come more often. Eyes that open too soon after a voice open silent; a lone voice that's due too soon
    /// waits. Counts every second, safe or not: it's about what the player hears. Plain C# (ADR 0003).
    /// </summary>
    public sealed class PhantomVoiceGap
    {
        private readonly float minSeconds;
        private float sinceLastSeconds = float.PositiveInfinity;

        /// <param name="minSeconds">Fewest seconds between two voices starting. 0: no gap.</param>
        public PhantomVoiceGap(float minSeconds)
        {
            this.minSeconds = minSeconds >= 0f
                ? minSeconds
                : throw new ArgumentOutOfRangeException(nameof(minSeconds), minSeconds, "This can't be negative.");
        }

        /// <summary>Whether a voice may start now.</summary>
        public bool IsOpen => sinceLastSeconds >= minSeconds;

        /// <summary>Seconds since the last voice started; infinite before the first.</summary>
        public float SinceLastSeconds => sinceLastSeconds;

        /// <summary>Moves time on by <paramref name="deltaSeconds"/>.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            sinceLastSeconds += deltaSeconds;
        }

        /// <summary>A voice started now: the next must wait the gap.</summary>
        public void Started()
        {
            sinceLastSeconds = 0f;
        }
    }
}
