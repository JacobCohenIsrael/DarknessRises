using System;

namespace DarknessRises.Game.Continues
{
    /// <summary>
    /// The arcade countdown under "CONTINUE?": it counts down from its first number to 0, one number at a time, and
    /// when 0 has had its turn the offer is gone. Stopping it (the player took the Continue) keeps it where it is.
    /// Plain C#: time arrives as an explicit delta (ADR 0003).
    /// </summary>
    public sealed class ContinueCountdown
    {
        private readonly int from;
        private readonly float secondsPerCount;
        private float secondsOnCount;

        /// <param name="from">The number the countdown starts at. It ends after 0.</param>
        /// <param name="secondsPerCount">Seconds each number stays up.</param>
        public ContinueCountdown(int from, float secondsPerCount)
        {
            if (from < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(from), from, "A countdown counts down to 0, not up to it.");
            }

            if (secondsPerCount <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(secondsPerCount), secondsPerCount, "Each number must stay up for some time.");
            }

            this.from = from;
            this.secondsPerCount = secondsPerCount;
        }

        /// <summary>Raised when the number changes, with the new one. Also for the first number, when the countdown starts.</summary>
        public event Action<int> CountChanged;

        /// <summary>Raised once, when 0 has had its turn.</summary>
        public event Action Expired;

        /// <summary>The number showing.</summary>
        public int Count { get; private set; }

        public bool IsRunning { get; private set; }

        /// <summary>Whether the countdown ran out since it last started.</summary>
        public bool IsExpired { get; private set; }

        /// <summary>The number it starts at.</summary>
        public int From => from;

        /// <summary>Starts counting down from the first number, afresh.</summary>
        public void Start()
        {
            IsRunning = true;
            IsExpired = false;
            secondsOnCount = 0f;
            Count = from;
            CountChanged?.Invoke(Count);
        }

        /// <summary>Stops counting where it is: the player took the Continue.</summary>
        public void Stop()
        {
            IsRunning = false;
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (!IsRunning)
            {
                return;
            }

            secondsOnCount += deltaSeconds;
            while (IsRunning && secondsOnCount >= secondsPerCount)
            {
                secondsOnCount -= secondsPerCount;
                if (Count == 0)
                {
                    IsRunning = false;
                    IsExpired = true;
                    Expired?.Invoke();
                    return;
                }

                Count--;
                CountChanged?.Invoke(Count);
            }
        }
    }
}
