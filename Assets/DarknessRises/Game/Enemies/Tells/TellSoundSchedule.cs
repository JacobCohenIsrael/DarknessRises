using System;
using DarknessRises.Game.Randomness;

namespace DarknessRises.Game.Enemies.Tells
{
    /// <summary>
    /// When a living Enemy makes its next Tell sound: at random intervals between a minimum and a maximum,
    /// starting a random interval after it spawns, so a Wave doesn't call out in unison. Plain C# (ADR 0003).
    /// </summary>
    public sealed class TellSoundSchedule
    {
        private readonly float minIntervalSeconds;
        private readonly float maxIntervalSeconds;
        private readonly IRandom random;
        private float secondsLeft;

        public TellSoundSchedule(float minIntervalSeconds, float maxIntervalSeconds, IRandom random)
        {
            if (minIntervalSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(minIntervalSeconds), minIntervalSeconds, "Intervals can't be negative.");
            }

            if (maxIntervalSeconds < minIntervalSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(maxIntervalSeconds), maxIntervalSeconds, "The longest interval can't be shorter than the shortest.");
            }

            this.minIntervalSeconds = minIntervalSeconds;
            this.maxIntervalSeconds = maxIntervalSeconds;
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public bool IsRunning { get; private set; }

        /// <summary>Starts counting down to the first sound.</summary>
        public void Start()
        {
            IsRunning = true;
            secondsLeft = NextInterval();
        }

        /// <summary>No more sounds until <see cref="Start"/> is called again.</summary>
        public void Stop()
        {
            IsRunning = false;
        }

        /// <summary>Advances time. True when a Tell sound is due now; the next one is then scheduled.</summary>
        public bool Tick(float deltaSeconds)
        {
            if (!IsRunning)
            {
                return false;
            }

            secondsLeft -= deltaSeconds;
            if (secondsLeft > 0f)
            {
                return false;
            }

            // Counted from now rather than carried over, so a long frame never makes a burst of sounds.
            secondsLeft = NextInterval();
            return true;
        }

        /// <summary>A random index into <paramref name="count"/> choices, such as which clip to play.</summary>
        public int PickIndex(int count)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "There must be something to pick.");
            }

            return Math.Min((int)random.Range(0f, count), count - 1);
        }

        private float NextInterval()
        {
            return random.Range(minIntervalSeconds, maxIntervalSeconds);
        }
    }
}
