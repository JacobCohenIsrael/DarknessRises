using System;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// The state of a Primary weapon: fire-rate gating, the magazine, manual and automatic reload, and the ammo reserve
    /// its reloads take from. Plain C#: time arrives as an explicit delta and commands as method calls (ADR 0003).
    /// </summary>
    /// <remarks>
    /// While the trigger is held the Weapon fires at its fire rate, starting on the tick the trigger is pulled
    /// if it's ready. Releasing the trigger never banks shots: the next pull fires as soon as the fire-rate gap
    /// since the last shot has passed. Firing the last shot in the magazine starts a reload automatically,
    /// and nothing fires while reloading. A reload fills the magazine as far as the reserve allows. With the magazine
    /// and the reserve both empty the Weapon has run dry: it can't fire or reload until its reserve is refilled.
    /// </remarks>
    public sealed class PrimaryWeapon
    {
        private readonly PrimaryWeaponStats stats;
        private float secondsUntilReady;
        private float reloadSecondsLeft;

        /// <param name="stats">Its fire rate, magazine and reload time.</param>
        /// <param name="reserve">The ammo its reloads take from. None: an unlimited reserve.</param>
        public PrimaryWeapon(PrimaryWeaponStats stats, AmmoReserve reserve = null)
        {
            this.stats = stats;
            Reserve = reserve ?? AmmoReserve.Unlimited();
            RoundsInMagazine = stats.MagazineSize;
        }

        /// <summary>Raised once for every shot, after the magazine count has dropped.</summary>
        public event Action Fired;

        /// <summary>Raised when a reload starts, manual or automatic.</summary>
        public event Action ReloadStarted;

        /// <summary>Raised when a reload finishes and the magazine is as full as the reserve could make it.</summary>
        public event Action ReloadFinished;

        /// <summary>Raised when the last round is fired with nothing left in the reserve: the Weapon has run dry.</summary>
        public event Action<PrimaryWeapon> RanDry;

        public int RoundsInMagazine { get; private set; }
        public int MagazineSize => stats.MagazineSize;
        public bool IsReloading { get; private set; }
        public bool IsTriggerHeld { get; private set; }

        /// <summary>The ammo beyond the magazine, which reloads take from.</summary>
        public AmmoReserve Reserve { get; }

        /// <summary>Whether the Weapon has anything to fire: rounds in the magazine, or in the reserve to reload from.</summary>
        public bool HasAmmo => RoundsInMagazine > 0 || !Reserve.IsEmpty;

        /// <summary>How far the current reload is, from 0 to 1. 0 when not reloading.</summary>
        public float ReloadProgress =>
            !IsReloading ? 0f
            : stats.ReloadSeconds <= 0f ? 1f
            : 1f - reloadSecondsLeft / stats.ReloadSeconds;

        /// <summary>Seconds a reload of this Weapon takes.</summary>
        public float ReloadSeconds => stats.ReloadSeconds;

        /// <summary>Seconds since the current reload started. 0 when not reloading.</summary>
        public float ReloadElapsedSeconds => IsReloading ? stats.ReloadSeconds - reloadSecondsLeft : 0f;

        /// <summary>Forgets whoever listened to the Weapon and its reserve: the Level that showed it is gone.</summary>
        public void DropListeners()
        {
            Fired = null;
            ReloadStarted = null;
            ReloadFinished = null;
            RanDry = null;
            Reserve.DropListeners();
        }

        public void PullTrigger()
        {
            IsTriggerHeld = true;
        }

        public void ReleaseTrigger()
        {
            IsTriggerHeld = false;
        }

        /// <summary>
        /// Starts a manual reload. Does nothing if the magazine is full, a reload is already running, or the reserve
        /// is empty.
        /// </summary>
        /// <returns>Whether a reload started.</returns>
        public bool Reload()
        {
            if (IsReloading || RoundsInMagazine >= stats.MagazineSize || Reserve.IsEmpty)
            {
                return false;
            }

            StartReload();
            return true;
        }

        /// <summary>Advances the Weapon by <paramref name="deltaSeconds"/>, firing as many shots as the fire rate allows.</summary>
        public void Tick(float deltaSeconds) => Tick(deltaSeconds, mayFire: true);

        /// <summary>
        /// Advances the Weapon by <paramref name="deltaSeconds"/>. With <paramref name="mayFire"/> false (the switch beat
        /// is lowering the gun before it) it reloads but holds its fire, and banks no shots for later.
        /// </summary>
        public void Tick(float deltaSeconds, bool mayFire)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time only moves forward.");
            }

            secondsUntilReady -= deltaSeconds;

            // An empty magazine reloads on its own as soon as there's ammo for it: after a refill, too.
            if (!IsReloading && RoundsInMagazine == 0 && !Reserve.IsEmpty)
            {
                StartReload();
            }
            else if (IsReloading)
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

            while (mayFire && IsTriggerHeld && !IsReloading && RoundsInMagazine > 0 && secondsUntilReady <= 0f)
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

            if (RoundsInMagazine > 0)
            {
                return;
            }

            if (Reserve.IsEmpty)
            {
                RanDry?.Invoke(this);
            }
            else
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
            RoundsInMagazine += Reserve.Take(stats.MagazineSize - RoundsInMagazine);
            ReloadFinished?.Invoke();
        }
    }
}
