using System;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// The flash on the Weapon button when a Primary weapon runs dry and the Pistol takes over: it shows at full
    /// strength at once and fades out over its time. Plain C#: time arrives as an explicit delta (ADR 0003).
    /// </summary>
    public sealed class RunDryFlash
    {
        private readonly float seconds;
        private float secondsLeft;

        /// <param name="seconds">Seconds the flash takes to fade out. 0 turns it off.</param>
        public RunDryFlash(float seconds)
        {
            if (!(seconds >= 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A flash can't take less than no time.");
            }

            this.seconds = seconds;
        }

        /// <summary>How strongly the flash shows: 1 as the Weapon runs dry, fading to 0.</summary>
        public float Strength => seconds > 0f ? secondsLeft / seconds : 0f;

        /// <summary>Whether there is still something of the flash to show.</summary>
        public bool IsFlashing => secondsLeft > 0f;

        /// <summary>A Weapon ran dry: the flash starts over at full strength.</summary>
        public void Start()
        {
            secondsLeft = seconds;
        }

        /// <summary>Fades the flash by <paramref name="deltaSeconds"/>.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time only moves forward.");
            }

            secondsLeft = Math.Max(0f, secondsLeft - deltaSeconds);
        }
    }
}
