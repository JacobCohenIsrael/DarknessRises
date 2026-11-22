using System;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// The arcade beat of the accuracy bonus on a Result card: the card shows the Station's Score, then after a short
    /// wait the bonus stamps in (it punches down from larger than life), then after another the Score counts up to the
    /// total. It says when each happens; whoever shows the card draws it.
    /// Plain C#: time arrives as an explicit delta (ADR 0003).
    /// </summary>
    public sealed class BonusReveal
    {
        private readonly float stampDelaySeconds;
        private readonly float stampSeconds;
        private readonly float stampScale;
        private readonly float countDelaySeconds;
        private float elapsedSeconds;
        private bool isRunning;

        /// <param name="stampDelaySeconds">From the card showing to the stamp.</param>
        /// <param name="stampSeconds">The stamp's punch down to its size.</param>
        /// <param name="stampScale">How many times its size the stamp starts at.</param>
        /// <param name="countDelaySeconds">From the stamp landing to the count starting.</param>
        /// <param name="countSeconds">How long the count up to the total takes.</param>
        public BonusReveal(float stampDelaySeconds, float stampSeconds, float stampScale, float countDelaySeconds, float countSeconds)
        {
            if (stampDelaySeconds < 0f || stampSeconds < 0f || countDelaySeconds < 0f || countSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(stampDelaySeconds), "A beat can't take less than no time.");
            }

            if (stampScale < 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(stampScale), stampScale, "A stamp punches down from larger than its size.");
            }

            this.stampDelaySeconds = stampDelaySeconds;
            this.stampSeconds = stampSeconds;
            this.stampScale = stampScale;
            this.countDelaySeconds = countDelaySeconds;
            CountSeconds = countSeconds;
            StampSize = 1f;
        }

        /// <summary>Raised once per reveal, when the stamp starts to show.</summary>
        public event Action Stamped;

        /// <summary>Raised once per reveal, when the count to the total starts. It takes <see cref="CountSeconds"/>.</summary>
        public event Action CountStarted;

        /// <summary>How long the count up to the total takes.</summary>
        public float CountSeconds { get; }

        /// <summary>Whether the stamp shows.</summary>
        public bool IsStamped { get; private set; }

        /// <summary>Whether the count to the total has started.</summary>
        public bool IsCounting { get; private set; }

        /// <summary>The stamp's size, as a multiple of its own: above 1 while it punches in.</summary>
        public float StampSize { get; private set; }

        /// <summary>The whole reveal's length: until the count lands on the total.</summary>
        public float TotalSeconds => stampDelaySeconds + stampSeconds + countDelaySeconds + CountSeconds;

        /// <summary>Starts the reveal over: nothing stamped, nothing counted.</summary>
        public void Begin()
        {
            elapsedSeconds = 0f;
            isRunning = true;
            IsStamped = false;
            IsCounting = false;
            StampSize = 1f;
            Tick(0f);
        }

        /// <summary>Stops the reveal where it is: a card with no bonus never stamps.</summary>
        public void Stop()
        {
            isRunning = false;
            IsStamped = false;
            IsCounting = false;
            StampSize = 1f;
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (!isRunning)
            {
                return;
            }

            elapsedSeconds += deltaSeconds;
            var sinceStamp = elapsedSeconds - stampDelaySeconds;
            if (sinceStamp < 0f)
            {
                return;
            }

            if (!IsStamped)
            {
                IsStamped = true;
                Stamped?.Invoke();
            }

            var punched = stampSeconds > 0f ? Math.Min(1f, sinceStamp / stampSeconds) : 1f;

            // Fast at first, landing hard.
            var landed = 1f - (1f - punched) * (1f - punched);
            StampSize = stampScale + (1f - stampScale) * landed;

            if (!IsCounting && sinceStamp >= stampSeconds + countDelaySeconds)
            {
                IsCounting = true;
                isRunning = false;
                StampSize = 1f;
                CountStarted?.Invoke();
            }
        }
    }
}
