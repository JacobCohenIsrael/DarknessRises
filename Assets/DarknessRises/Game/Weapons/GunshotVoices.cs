using System;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// Which of a small, fixed set of voices (AudioSources) each Gunshot plays on, so rapid fire overlaps without
    /// cutting earlier shots and never takes more voices than the set holds. Plain C# (ADR 0003).
    /// </summary>
    /// <remarks>
    /// A Gunshot goes to a free voice, the one that has been free longest. When every voice is busy it takes the
    /// oldest one: the only way a Gunshot is ever cut short. Each voice ends after its Gunshot's length and fades out over
    /// its last moment (<see cref="Gain"/>), so a trimmed clip never stops dead.
    /// </remarks>
    public sealed class GunshotVoices
    {
        private readonly float[] secondsLeft;
        private readonly long[] startedAt;
        private long starts;

        /// <param name="count">How many voices there are.</param>
        public GunshotVoices(int count)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Gunshots need at least one voice.");
            }

            secondsLeft = new float[count];
            startedAt = new long[count];
            for (var i = 0; i < count; i++)
            {
                // Never started: free, and free the longest, in order.
                startedAt[i] = i - count;
            }
        }

        public int Count => secondsLeft.Length;

        /// <summary>How many voices are playing a Gunshot.</summary>
        public int PlayingCount
        {
            get
            {
                var playing = 0;
                for (var i = 0; i < secondsLeft.Length; i++)
                {
                    if (secondsLeft[i] > 0f)
                    {
                        playing++;
                    }
                }

                return playing;
            }
        }

        /// <summary>How many Gunshots were cut short because every voice was busy.</summary>
        public int StolenCount { get; private set; }

        /// <summary>
        /// Plays a Gunshot that lasts <paramref name="seconds"/> of real time.
        /// </summary>
        /// <returns>The voice it plays on.</returns>
        public int Start(float seconds)
        {
            if (seconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A Gunshot can't last negative time.");
            }

            var voice = OldestFree();
            if (voice < 0)
            {
                voice = Oldest();
                StolenCount++;
            }

            secondsLeft[voice] = seconds;
            startedAt[voice] = starts++;
            return voice;
        }

        /// <summary>Advances every playing voice by <paramref name="deltaSeconds"/> of real time.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time only moves forward.");
            }

            for (var i = 0; i < secondsLeft.Length; i++)
            {
                if (secondsLeft[i] > 0f)
                {
                    secondsLeft[i] = Math.Max(0f, secondsLeft[i] - deltaSeconds);
                }
            }
        }

        public bool IsPlaying(int voice)
        {
            return secondsLeft[voice] > 0f;
        }

        public float SecondsLeft(int voice)
        {
            return secondsLeft[voice];
        }

        /// <summary>
        /// How loud <paramref name="voice"/> is relative to its Gunshot's volume, from 1 down to 0 over its last
        /// <paramref name="endFadeSeconds"/>. 0 once it has ended.
        /// </summary>
        public float Gain(int voice, float endFadeSeconds)
        {
            var left = secondsLeft[voice];
            if (left <= 0f)
            {
                return 0f;
            }

            return endFadeSeconds <= 0f || left >= endFadeSeconds ? 1f : left / endFadeSeconds;
        }

        private int OldestFree()
        {
            var found = -1;
            for (var i = 0; i < secondsLeft.Length; i++)
            {
                if (secondsLeft[i] <= 0f && (found < 0 || startedAt[i] < startedAt[found]))
                {
                    found = i;
                }
            }

            return found;
        }

        private int Oldest()
        {
            var found = 0;
            for (var i = 1; i < startedAt.Length; i++)
            {
                if (startedAt[i] < startedAt[found])
                {
                    found = i;
                }
            }

            return found;
        }
    }
}
