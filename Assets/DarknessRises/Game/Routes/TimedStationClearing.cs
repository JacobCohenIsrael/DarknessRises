using System;

namespace DarknessRises.Game.Routes
{
    /// <summary>
    /// Stand-in for a Station's fight until Waves exist: clears each Station a fixed hold time after the character
    /// reaches it, or at once when the hold time is zero. The Wave schedule replaces this.
    /// </summary>
    public sealed class TimedStationClearing : IDisposable
    {
        private readonly RouteProgress progress;
        private readonly float holdSeconds;
        private Station holding;
        private float remainingSeconds;

        public TimedStationClearing(RouteProgress progress, float holdSeconds)
        {
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));

            if (holdSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(holdSeconds), holdSeconds, "Hold time can't be negative.");
            }

            this.holdSeconds = holdSeconds;
            progress.StationReached += OnStationReached;
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (holding == null)
            {
                return;
            }

            remainingSeconds -= deltaSeconds;
            if (remainingSeconds > 0f)
            {
                return;
            }

            var station = holding;
            holding = null;
            station.Clear();
        }

        public void Dispose()
        {
            progress.StationReached -= OnStationReached;
            holding = null;
        }

        private void OnStationReached(Station station)
        {
            if (holdSeconds <= 0f)
            {
                station.Clear();
                return;
            }

            holding = station;
            remainingSeconds = holdSeconds;
        }
    }
}
