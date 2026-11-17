using System;

namespace DarknessRises.Game.Player
{
    /// <summary>
    /// A one-shot animation whose gameplay moments are authored data, such as the throw (with the moment the
    /// Grenade leaves the hand as its cue) or death. Time comes in as an explicit delta.
    /// </summary>
    public sealed class OneShotMotion
    {
        private readonly float cueSeconds;
        private readonly float durationSeconds;
        private float elapsedSeconds;
        private bool cueRaised;

        public OneShotMotion(float cueSeconds, float durationSeconds)
        {
            if (durationSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), durationSeconds, "A motion can't last less than no time.");
            }

            if (cueSeconds < 0f || cueSeconds > durationSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(cueSeconds), cueSeconds, "The cue must fall within the motion.");
            }

            this.cueSeconds = cueSeconds;
            this.durationSeconds = durationSeconds;
        }

        /// <summary>Raised once per play, when the motion reaches its cue (for the throw, the Grenade's release).</summary>
        public event Action Cued;

        /// <summary>Raised once per play, when the motion ends.</summary>
        public event Action Finished;

        public bool IsPlaying { get; private set; }

        /// <summary>Starts the motion from the beginning. Does nothing while it's already playing.</summary>
        /// <returns>Whether it started.</returns>
        public bool TryPlay()
        {
            if (IsPlaying)
            {
                return false;
            }

            IsPlaying = true;
            elapsedSeconds = 0f;
            cueRaised = false;
            return true;
        }

        /// <summary>Advances the motion. One long step can reach the cue and the end; the cue is raised first.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (!IsPlaying)
            {
                return;
            }

            elapsedSeconds += deltaSeconds;

            if (!cueRaised && elapsedSeconds >= cueSeconds)
            {
                cueRaised = true;
                Cued?.Invoke();
            }

            if (elapsedSeconds >= durationSeconds)
            {
                IsPlaying = false;
                Finished?.Invoke();
            }
        }
    }
}
