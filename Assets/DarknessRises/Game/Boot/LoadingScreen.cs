using System;

namespace DarknessRises.Game.Boot
{
    /// <summary>
    /// What the loading screen shows over time. It covers the screen for as long as nothing is ready to be seen: from
    /// boot until the first Level or the Shop is ready, and through every change between them. It's plain black at
    /// first, so a quick change between Levels stays the black cut it always was; if the wait goes on (a first visit
    /// still downloading its content), a label comes up, with dots that grow so it's plainly not stuck.
    /// Plain C#: time arrives as an explicit delta (ADR 0003).
    /// </summary>
    public sealed class LoadingScreen
    {
        private readonly float labelDelaySeconds;
        private readonly float dotSeconds;
        private readonly int maxDots;
        private float secondsShown;

        /// <param name="labelDelaySeconds">Seconds the screen stays plain black before the label comes up.</param>
        /// <param name="dotSeconds">Seconds each dot after the label takes to appear.</param>
        /// <param name="maxDots">How many dots follow the label before they start over.</param>
        public LoadingScreen(float labelDelaySeconds, float dotSeconds, int maxDots)
        {
            if (!(labelDelaySeconds >= 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(labelDelaySeconds), labelDelaySeconds, "The label can't come up before the screen does.");
            }

            if (!(dotSeconds > 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(dotSeconds), dotSeconds, "A dot takes some time to appear.");
            }

            if (maxDots < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDots), maxDots, "There can't be fewer than no dots.");
            }

            this.labelDelaySeconds = labelDelaySeconds;
            this.dotSeconds = dotSeconds;
            this.maxDots = maxDots;
        }

        /// <summary>Whether the screen is covered.</summary>
        public bool IsShown { get; private set; }

        /// <summary>Whether the wait has gone on long enough for the label to show.</summary>
        public bool IsLabelShown => IsShown && secondsShown >= labelDelaySeconds;

        /// <summary>How many dots follow the label now: none as it comes up, then one more each beat, then none again.</summary>
        public int Dots => IsLabelShown ? (int)((secondsShown - labelDelaySeconds) / dotSeconds) % (maxDots + 1) : 0;

        /// <summary>
        /// Covers the screen: something is loading and there is nothing ready to see. Asked again while it's already
        /// up, it carries on as it was, so one wait made of several loads counts as one.
        /// </summary>
        public void Show()
        {
            if (IsShown)
            {
                return;
            }

            IsShown = true;
            secondsShown = 0f;
        }

        /// <summary>Uncovers the screen: what was loading is ready.</summary>
        public void Hide()
        {
            IsShown = false;
            secondsShown = 0f;
        }

        /// <summary>Advances the wait by <paramref name="deltaSeconds"/>.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time only moves forward.");
            }

            if (IsShown)
            {
                secondsShown += deltaSeconds;
            }
        }
    }
}
