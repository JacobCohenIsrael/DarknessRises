using System;

namespace DarknessRises.Game.Scoring
{
    /// <summary>
    /// The Combo: a streak of consecutive hits that multiplies the Score of every kill. Every few hits in a row the
    /// multiplier steps up, to a cap. A miss ends the streak, and so does going too long without a hit.
    /// Plain C#: time arrives as an explicit delta (ADR 0003).
    /// </summary>
    public sealed class Combo
    {
        private readonly int hitsPerStep;
        private readonly int maxMultiplier;
        private readonly float timeoutSeconds;

        /// <param name="hitsPerStep">Consecutive hits it takes to raise the multiplier by one.</param>
        /// <param name="maxMultiplier">The highest the multiplier goes. 1 switches the Combo off.</param>
        /// <param name="timeoutSeconds">Seconds without a hit after which the streak ends. 0 never times out.</param>
        public Combo(int hitsPerStep, int maxMultiplier, float timeoutSeconds)
        {
            if (hitsPerStep < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(hitsPerStep), hitsPerStep, "A step takes at least one hit.");
            }

            if (maxMultiplier < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxMultiplier), maxMultiplier, "A multiplier never goes below 1.");
            }

            if (timeoutSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), timeoutSeconds, "A timeout can't be negative.");
            }

            this.hitsPerStep = hitsPerStep;
            this.maxMultiplier = maxMultiplier;
            this.timeoutSeconds = timeoutSeconds;
        }

        /// <summary>Raised when <see cref="Multiplier"/> changes, with the new one.</summary>
        public event Action<int> MultiplierChanged;

        /// <summary>Consecutive hits so far.</summary>
        public int Streak { get; private set; }

        /// <summary>What a kill's points are multiplied by right now: 1 with no streak.</summary>
        public int Multiplier => Math.Min(maxMultiplier, 1 + Streak / hitsPerStep);

        /// <summary>Seconds left before the streak times out. 0 with no streak, or with no timeout.</summary>
        public float SecondsLeft { get; private set; }

        /// <summary>The share of the timeout still left, from 1 (just hit) to 0. 0 with no streak; 1 with no timeout.</summary>
        public float TimeLeftFraction => Streak == 0 ? 0f : timeoutSeconds > 0f ? SecondsLeft / timeoutSeconds : 1f;

        /// <summary>A Shot hurt an Enemy: the streak grows and its timeout starts over.</summary>
        public void Hit()
        {
            var before = Multiplier;
            Streak++;
            SecondsLeft = timeoutSeconds;
            Announce(before);
        }

        /// <summary>A Shot hurt nothing: the streak ends.</summary>
        public void Miss()
        {
            End();
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (Streak == 0 || timeoutSeconds <= 0f)
            {
                return;
            }

            SecondsLeft -= deltaSeconds;
            if (SecondsLeft <= 0f)
            {
                End();
            }
        }

        private void End()
        {
            var before = Multiplier;
            Streak = 0;
            SecondsLeft = 0f;
            Announce(before);
        }

        private void Announce(int before)
        {
            var now = Multiplier;
            if (now != before)
            {
                MultiplierChanged?.Invoke(now);
            }
        }
    }
}
