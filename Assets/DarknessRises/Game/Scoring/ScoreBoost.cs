using System;

namespace DarknessRises.Game.Scoring
{
    /// <summary>
    /// The Score boost: a rewarded ad, offered on a Station's Result card and watched to the end, multiplies every Score
    /// the player earns (each kill after the Combo, and so each Station's accuracy bonus) for a set time of play.
    /// It's offered whenever none is running and the Run hasn't used up its authored allowance (none by default).
    /// Plain C# (ADR 0003): time arrives as an explicit delta, and whoever runs the Level ticks it only while the game
    /// plays, so it stands still while paused, while an ad plays, on loading screens and while the character is dead.
    /// One lives for a whole Run and carries from Level to Level; it ends when its time is up or the Run ends.
    /// </summary>
    public sealed class ScoreBoost
    {
        private readonly int factor;
        private readonly float durationSeconds;
        private readonly int maxPerRun;

        /// <param name="factor">What every Score is multiplied by while it runs. 2 doubles it.</param>
        /// <param name="durationSeconds">Seconds of play one boost lasts.</param>
        /// <param name="maxPerRun">How many boosts a Run may have. 0 for no limit.</param>
        public ScoreBoost(int factor, float durationSeconds, int maxPerRun)
        {
            if (factor < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(factor), factor, "A boost never shrinks the Score.");
            }

            if (durationSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), durationSeconds, "A boost lasts some time.");
            }

            if (maxPerRun < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxPerRun), maxPerRun, "0 means no limit; fewer makes no sense.");
            }

            this.factor = factor;
            this.durationSeconds = durationSeconds;
            this.maxPerRun = maxPerRun;
        }

        /// <summary>Raised when a boost starts.</summary>
        public event Action Started;

        /// <summary>Raised when a running boost ends: its time ran out, or the Run ended.</summary>
        public event Action Ended;

        /// <summary>What every Score is multiplied by while a boost runs.</summary>
        public int Factor => factor;

        /// <summary>Seconds of play one boost lasts.</summary>
        public float DurationSeconds => durationSeconds;

        public bool IsRunning { get; private set; }

        /// <summary>Seconds of play left on the running boost; 0 with none running.</summary>
        public float SecondsLeft { get; private set; }

        /// <summary>Whether the last boost ended because its time ran out, rather than with the Run.</summary>
        public bool TimeRanOut { get; private set; }

        /// <summary>How many boosts this Run has had.</summary>
        public int StartedCount { get; private set; }

        /// <summary>
        /// Whether a Result card offers the boost now: none is running (the player's rule: no button while one runs)
        /// and the Run hasn't had as many as it may.
        /// </summary>
        public bool IsOffered => !IsRunning && (maxPerRun == 0 || StartedCount < maxPerRun);

        /// <summary>What a Score of <paramref name="points"/> earns now: multiplied while a boost runs.</summary>
        public int Apply(int points)
        {
            if (!IsRunning || points <= 0)
            {
                return points;
            }

            return (int)Math.Min(int.MaxValue, (long)points * factor);
        }

        /// <summary>The ad was watched to the end: the boost starts, if it's on offer.</summary>
        /// <returns>Whether it started.</returns>
        public bool Start()
        {
            if (!IsOffered)
            {
                return false;
            }

            StartedCount++;
            IsRunning = true;
            SecondsLeft = durationSeconds;
            Started?.Invoke();
            return true;
        }

        /// <summary>Runs the boost's time down by <paramref name="deltaSeconds"/> of play.</summary>
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

            SecondsLeft -= deltaSeconds;
            if (SecondsLeft <= 0f)
            {
                Finish(timeRanOut: true);
            }
        }

        /// <summary>Whether the running boost has <paramref name="warningSeconds"/> or less left: the HUD pulses it.</summary>
        public bool IsEnding(float warningSeconds) => IsRunning && SecondsLeft <= warningSeconds;

        /// <summary>The Run ended: a running boost ends with it.</summary>
        public void End() => Finish(timeRanOut: false);

        private void Finish(bool timeRanOut)
        {
            if (!IsRunning)
            {
                return;
            }

            TimeRanOut = timeRanOut;
            IsRunning = false;
            SecondsLeft = 0f;
            Ended?.Invoke();
        }
    }
}
