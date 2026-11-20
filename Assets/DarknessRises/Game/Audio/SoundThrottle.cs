using System;

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// Keeps a sound that can be asked for many times a second (a hit on the Boss, once per Shot) from playing more often
    /// than an authored minimum interval, so it adds to the sounds around it instead of drowning them out. Plain C#
    /// (ADR 0003); a view ticks it and asks <see cref="TryPlay"/> before each play.
    /// </summary>
    public sealed class SoundThrottle
    {
        private readonly float minIntervalSeconds;
        private float secondsSinceLastPlay;

        /// <param name="minIntervalSeconds">The least time between two plays. 0 lets every play through.</param>
        public SoundThrottle(float minIntervalSeconds)
        {
            if (minIntervalSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(minIntervalSeconds), minIntervalSeconds, "An interval can't be negative.");
            }

            this.minIntervalSeconds = minIntervalSeconds;
            secondsSinceLastPlay = float.PositiveInfinity;
        }

        /// <summary>Advances time by <paramref name="deltaSeconds"/>.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            secondsSinceLastPlay += deltaSeconds;
        }

        /// <summary>
        /// Whether the sound may play now: true the first time, then once at least the interval has passed since the last
        /// play it let through. A play it lets through starts the interval again; one it holds back doesn't.
        /// </summary>
        public bool TryPlay()
        {
            if (secondsSinceLastPlay < minIntervalSeconds)
            {
                return false;
            }

            secondsSinceLastPlay = 0f;
            return true;
        }
    }
}
