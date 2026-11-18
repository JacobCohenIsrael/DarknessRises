using System;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// The state of the Primary weapon: fire-rate gating, the magazine, and manual and automatic reload.
    /// Plain C#: time arrives as an explicit delta and commands as method calls (ADR 0003).
    /// </summary>
    /// <remarks>
    /// While the trigger is held the Weapon fires at its fire rate, starting on the tick the trigger is pulled
    /// if it's ready. Releasing the trigger never banks shots: the next pull fires as soon as the fire-rate gap
    /// since the last shot has passed. Firing the last shot in the magazine starts a reload automatically,
    /// and nothing fires while reloading. A reload always refills the whole magazine.
    /// </remarks>
    public sealed class PrimaryWeapon
    {
        private readonly PrimaryWeaponStats stats;
        private float secondsUntilReady;
        private float reloadSecondsLeft;

        public PrimaryWeapon(PrimaryWeaponStats stats)
        {
            this.stats = stats;
            RoundsInMagazine = stats.MagazineSize;
        }

        /// <summary>Raised once for every shot, after the magazine count has dropped.</summary>
        public event Action Fired;

        /// <summary>Raised when a reload starts, manual or automatic.</summary>
        public event Action ReloadStarted;

        /// <summary>Raised when a reload finishes and the magazine is full again.</summary>
        public event Action ReloadFinished;

        public int RoundsInMagazine { get; private set; }
        public int MagazineSize => stats.MagazineSize;
        public bool IsReloading { get; private set; }
        public bool IsTriggerHeld { get; private set; }

        /// <summary>How far the current reload is, from 0 to 1. 0 when not reloading.</summary>
        public float ReloadProgress =>
            !IsReloading ? 0f
            : stats.ReloadSeconds <= 0f ? 1f
            : 1f - reloadSecondsLeft / stats.ReloadSeconds;

        /// <summary>Seconds a reload of this Weapon takes.</summary>
        public float ReloadSeconds => stats.ReloadSeconds;

        /// <summary>Seconds since the current reload started. 0 when not reloading.</summary>
        public float ReloadElapsedSeconds => IsReloading ? stats.ReloadSeconds - reloadSecondsLeft : 0f;

        public void PullTrigger()
        {
            IsTriggerHeld = true;
        }

        public void ReleaseTrigger()
        {
            IsTriggerHeld = false;
        }

        /// <summary>Starts a manual reload. Does nothing if the magazine is full or a reload is already running.</summary>
        /// <returns>Whether a reload started.</returns>
        public bool Reload()
        {
            if (IsReloading || RoundsInMagazine >= stats.MagazineSize)
            {
                return false;
            }

            StartReload();
            return true;
        }

        /// <summary>Advances the Weapon by <paramref name="deltaSeconds"/>, firing as many shots as the fire rate allows.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time only moves forward.");
            }

            secondsUntilReady -= deltaSeconds;

            if (IsReloading)
            {
                reloadSecondsLeft -= deltaSeconds;

                // Never ready before the reload ends, so time spent reloading isn't banked as extra shots either.
                if (secondsUntilReady < reloadSecondsLeft)
                {
                    secondsUntilReady = reloadSecondsLeft;
                }

                if (reloadSecondsLeft <= 0f)
                {
                    FinishReload();
                }
            }

            while (IsTriggerHeld && !IsReloading && secondsUntilReady <= 0f)
            {
                Fire();
                secondsUntilReady += stats.SecondsBetweenShots;
            }

            // Time spent not firing is never banked as extra shots.
            if (secondsUntilReady < 0f)
            {
                secondsUntilReady = 0f;
            }
        }

        private void Fire()
        {
            RoundsInMagazine--;
            Fired?.Invoke();

            if (RoundsInMagazine == 0)
            {
                StartReload();
            }
        }

        private void StartReload()
        {
            IsReloading = true;
            reloadSecondsLeft = stats.ReloadSeconds;
            ReloadStarted?.Invoke();
        }

        private void FinishReload()
        {
            IsReloading = false;
            reloadSecondsLeft = 0f;
            RoundsInMagazine = stats.MagazineSize;
            ReloadFinished?.Invoke();
        }
    }
}
