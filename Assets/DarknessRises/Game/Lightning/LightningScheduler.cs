using System;
using DarknessRises.Game.Randomness;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// Decides when Lightning flashes and when the Rumble before it starts.
    /// <para>
    /// In <see cref="LightningMode.Ambient"/> mode it flashes at random intervals within the configured range,
    /// with the Rumble a fixed lead before each flash, and only while the current Room lets Lightning in.
    /// In <see cref="LightningMode.BossDriven"/> mode it flashes only when the Boss pattern calls <see cref="RequestFlash()"/>.
    /// </para>
    /// Time is passed in explicitly through <see cref="Tick"/>; it never reads Unity time.
    /// </summary>
    public sealed class LightningScheduler
    {
        private readonly LightningTiming timing;
        private readonly IRandom random;

        private bool flashPending;
        private bool rumbled;
        private float secondsUntilFlash;
        private float rumbleLeadSeconds;

        public LightningScheduler(LightningTiming timing, IRandom random)
        {
            this.timing = timing;
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>The Rumble starts. The argument is the number of seconds until the flash.</summary>
        public event Action<float> Rumbled;

        /// <summary>Lightning flashes.</summary>
        public event Action Flashed;

        public LightningMode Mode { get; private set; } = LightningMode.Ambient;

        /// <summary>Whether the current Room lets Lightning in. No Room has been entered yet, so it starts false.</summary>
        public bool RoomLetsLightningIn { get; private set; }

        /// <summary>False while Lightning is switched off (the debug toggle). Nothing flashes or rumbles.</summary>
        public bool IsEnabled { get; private set; } = true;

        public bool IsFlashPending => flashPending;

        /// <summary>Seconds until the next flash, or infinity when none is coming.</summary>
        public float SecondsUntilFlash => flashPending ? secondsUntilFlash : float.PositiveInfinity;

        private bool AmbientActive => Mode == LightningMode.Ambient && IsEnabled && RoomLetsLightningIn;

        /// <summary>
        /// The character is now in a Room that does or doesn't let Lightning in. Walking from one Room with windows
        /// into another keeps the weather going without resetting the next flash.
        /// </summary>
        public void EnterRoom(bool letsLightningIn)
        {
            RoomLetsLightningIn = letsLightningIn;
            UpdateAmbientSchedule();
        }

        /// <summary>Switches Lightning on or off. Switching it off cancels a coming flash.</summary>
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
                CancelFlash();
            }
        }

        /// <summary>Hands Lightning back to the weather. A flash the Boss requested is cancelled.</summary>
        public void EnterAmbientMode()
        {
            if (Mode == LightningMode.Ambient)
            {
                return;
            }

            Mode = LightningMode.Ambient;
            CancelFlash();
            UpdateAmbientSchedule();
        }

        /// <summary>Hands Lightning to the Boss. A coming ambient flash is cancelled.</summary>
        public void EnterBossDrivenMode()
        {
            if (Mode == LightningMode.BossDriven)
            {
                return;
            }

            Mode = LightningMode.BossDriven;
            CancelFlash();
        }

        /// <summary>Boss mode: start the Rumble now and flash after the configured Rumble lead.</summary>
        public void RequestFlash()
        {
            RequestFlash(timing.RumbleLeadSeconds);
        }

        /// <summary>
        /// Boss mode: start the Rumble now and flash after <paramref name="rumbleLeadSeconds"/>.
        /// A lead of zero flashes at once. A new request replaces one that hasn't flashed yet.
        /// </summary>
        public void RequestFlash(float rumbleLeadSeconds)
        {
            if (Mode != LightningMode.BossDriven)
            {
                throw new InvalidOperationException("Only the Boss can request a flash; ambient Lightning keeps its own time.");
            }

            if (rumbleLeadSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(rumbleLeadSeconds), rumbleLeadSeconds, "The Rumble lead can't be negative.");
            }

            if (!IsEnabled)
            {
                return;
            }

            Schedule(rumbleLeadSeconds, rumbleLeadSeconds);
            Advance(0f);
        }

        /// <summary>Advances time, raising <see cref="Rumbled"/> and <see cref="Flashed"/> as they come due, in order.</summary>
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
            if (!flashPending)
            {
                return;
            }

            secondsUntilFlash -= deltaSeconds;

            // Handlers may change the schedule, so every step re-reads it.
            while (flashPending)
            {
                if (!rumbled && secondsUntilFlash <= rumbleLeadSeconds)
                {
                    rumbled = true;
                    Rumbled?.Invoke(secondsUntilFlash > 0f ? secondsUntilFlash : 0f);
                    continue;
                }

                if (secondsUntilFlash > 0f)
                {
                    return;
                }

                // A long frame can overshoot the flash; the next interval starts from when the flash was due.
                var overshootSeconds = -secondsUntilFlash;
                flashPending = false;
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
                CancelFlash();
            }
            else if (!flashPending)
            {
                ScheduleAmbientFlash(0f);
            }
        }

        private void ScheduleAmbientFlash(float elapsedSeconds)
        {
            var interval = random.Range(timing.MinIntervalSeconds, timing.MaxIntervalSeconds);
            Schedule(interval - elapsedSeconds, timing.RumbleLeadSeconds);
        }

        private void Schedule(float untilFlashSeconds, float leadSeconds)
        {
            flashPending = true;
            rumbled = false;
            secondsUntilFlash = untilFlashSeconds;
            rumbleLeadSeconds = leadSeconds;
        }

        private void CancelFlash()
        {
            flashPending = false;
            rumbled = false;
        }
    }
}
