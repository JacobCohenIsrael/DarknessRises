using System;
using DarknessRises.Game.Randomness;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// Decides when Lightning flashes and when the Rumble after it comes.
    /// <para>
    /// In <see cref="LightningMode.Ambient"/> mode it flashes at random intervals within the configured range, only while
    /// the current Room lets Lightning in, and nothing sounds before a flash.
    /// In <see cref="LightningMode.BossDriven"/> mode it flashes only when the Boss pattern calls
    /// <see cref="RequestFlash"/>, and the Boss's warning (its growl) comes first.
    /// In both, every flash is followed by its Rumble after a random delay within the configured range, like thunder from
    /// a strike at a varying distance.
    /// </para>
    /// Time is passed in explicitly through <see cref="Tick"/>; it never reads Unity time.
    /// </summary>
    public sealed class LightningScheduler
    {
        private readonly LightningTiming timing;
        private readonly IRandom random;

        private bool flashPending;
        private float secondsUntilFlash;
        private bool rumblePending;
        private float secondsUntilRumble;

        public LightningScheduler(LightningTiming timing, IRandom random)
        {
            this.timing = timing;
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>Boss mode: the Boss's warning starts. The argument is the number of seconds until the flash.</summary>
        public event Action<float> Warned;

        /// <summary>Lightning flashes.</summary>
        public event Action Flashed;

        /// <summary>The Rumble (the thunder) of the last flash starts.</summary>
        public event Action Rumbled;

        public LightningMode Mode { get; private set; } = LightningMode.Ambient;

        /// <summary>Whether the current Room lets Lightning in. No Room has been entered yet, so it starts false.</summary>
        public bool RoomLetsLightningIn { get; private set; }

        /// <summary>False while Lightning is switched off (the debug toggle). Nothing flashes or rumbles.</summary>
        public bool IsEnabled { get; private set; } = true;

        public bool IsFlashPending => flashPending;

        /// <summary>Seconds until the next flash, or infinity when none is coming.</summary>
        public float SecondsUntilFlash => flashPending ? secondsUntilFlash : float.PositiveInfinity;

        public bool IsRumblePending => rumblePending;

        /// <summary>Seconds until the Rumble of the last flash, or infinity when none is coming.</summary>
        public float SecondsUntilRumble => rumblePending ? secondsUntilRumble : float.PositiveInfinity;

        /// <summary>
        /// Whether the storm reaches the character, so its bed should be heard: Lightning switched on, and either a Room
        /// that lets it in or a Boss fight.
        /// </summary>
        public bool IsStormRunning => IsEnabled && (Mode == LightningMode.BossDriven || RoomLetsLightningIn);

        private bool AmbientActive => Mode == LightningMode.Ambient && IsEnabled && RoomLetsLightningIn;

        /// <summary>
        /// The character is now in a Room that does or doesn't let Lightning in. Walking from one Room with windows
        /// into another keeps the weather going without resetting the next flash. Outside a Boss fight, a Room without
        /// windows hears no Rumble either.
        /// </summary>
        public void EnterRoom(bool letsLightningIn)
        {
            RoomLetsLightningIn = letsLightningIn;
            if (!letsLightningIn && Mode == LightningMode.Ambient)
            {
                rumblePending = false;
            }

            UpdateAmbientSchedule();
        }

        /// <summary>Switches Lightning on or off. Switching it off cancels a coming flash and Rumble.</summary>
        public void SetEnabled(bool enabled)
        {
            if (IsEnabled == enabled)
            {
                return;
            }

            IsEnabled = enabled;
            if (enabled)
            {
                UpdateAmbientSchedule();
            }
            else
            {
                flashPending = false;
                rumblePending = false;
            }
        }

        /// <summary>
        /// Hands Lightning back to the weather. A flash the Boss requested is cancelled; the Rumble of one that already
        /// struck still comes.
        /// </summary>
        public void EnterAmbientMode()
        {
            if (Mode == LightningMode.Ambient)
            {
                return;
            }

            Mode = LightningMode.Ambient;
            flashPending = false;
            UpdateAmbientSchedule();
        }

        /// <summary>
        /// Hands Lightning to the Boss. A coming ambient flash is cancelled; the Rumble of one that already struck still comes.
        /// </summary>
        public void EnterBossDrivenMode()
        {
            if (Mode == LightningMode.BossDriven)
            {
                return;
            }

            Mode = LightningMode.BossDriven;
            flashPending = false;
        }

        /// <summary>
        /// Boss mode: the Boss's warning starts now and Lightning flashes after <paramref name="warningSeconds"/>.
        /// A warning of zero flashes at once. A new request replaces one that hasn't flashed yet.
        /// </summary>
        public void RequestFlash(float warningSeconds)
        {
            if (Mode != LightningMode.BossDriven)
            {
                throw new InvalidOperationException("Only the Boss can request a flash; ambient Lightning keeps its own time.");
            }

            if (warningSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(warningSeconds), warningSeconds, "The warning can't be negative.");
            }

            if (!IsEnabled)
            {
                return;
            }

            flashPending = true;
            secondsUntilFlash = warningSeconds;
            Warned?.Invoke(warningSeconds);
            Advance(0f);
        }

        /// <summary>Advances time, raising <see cref="Flashed"/> and <see cref="Rumbled"/> as they come due, in order.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            Advance(deltaSeconds);
        }

        private void Advance(float deltaSeconds)
        {
            if (flashPending)
            {
                secondsUntilFlash -= deltaSeconds;
            }

            if (rumblePending)
            {
                secondsUntilRumble -= deltaSeconds;
            }

            // Handlers may change the schedule, so every step re-reads it. A long frame can cover several flashes and
            // Rumbles; they come out in the order they were due.
            while (true)
            {
                var flashDue = flashPending && secondsUntilFlash <= 0f;
                var rumbleDue = rumblePending && secondsUntilRumble <= 0f;
                if (!flashDue && !rumbleDue)
                {
                    return;
                }

                if (rumbleDue && (!flashDue || secondsUntilRumble <= secondsUntilFlash))
                {
                    rumblePending = false;
                    Rumbled?.Invoke();
                    continue;
                }

                // The next interval and the Rumble count from when the flash was due, not from the end of a long frame.
                var overshootSeconds = -secondsUntilFlash;
                flashPending = false;

                // A flash while the last one's Rumble is still coming shares that Rumble.
                if (!rumblePending)
                {
                    rumblePending = true;
                    secondsUntilRumble =
                        random.Range(timing.MinRumbleDelaySeconds, timing.MaxRumbleDelaySeconds) - overshootSeconds;
                }

                Flashed?.Invoke();

                if (!flashPending && AmbientActive)
                {
                    ScheduleAmbientFlash(overshootSeconds);
                }
            }
        }

        private void UpdateAmbientSchedule()
        {
            if (Mode != LightningMode.Ambient)
            {
                return;
            }

            if (!AmbientActive)
            {
                flashPending = false;
            }
            else if (!flashPending)
            {
                ScheduleAmbientFlash(0f);
            }
        }

        private void ScheduleAmbientFlash(float elapsedSeconds)
        {
            flashPending = true;
            secondsUntilFlash = random.Range(timing.MinIntervalSeconds, timing.MaxIntervalSeconds) - elapsedSeconds;
        }
    }
}
