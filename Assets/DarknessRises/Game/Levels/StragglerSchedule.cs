using System;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Randomness;
using DarknessRises.Game.Waves;

namespace DarknessRises.Game.Levels
{
    /// <summary>
    /// Sends lone Stragglers at the character while it walks between Stations. Plain C#: time arrives as an
    /// explicit delta (ADR 0003).
    /// </summary>
    /// <remarks>
    /// The clock only runs while the character walks. Each Straggler comes a random interval after the last
    /// (within the Room's range), unless as many as the Room allows are already alive; then the interval starts over.
    /// When the spawner can't bring one in (no dark Spawn point ahead), it tries again every tick.
    /// </remarks>
    public sealed class StragglerSchedule<TEnemyType>
    {
        private readonly IEnemySpawner<TEnemyType> spawner;
        private readonly IRandom random;
        private readonly Action<Enemy> onStragglerDied;

        private StragglerPlan<TEnemyType> plan;
        private float secondsUntilNext;

        public StragglerSchedule(IEnemySpawner<TEnemyType> spawner, IRandom random)
        {
            this.spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            onStragglerDied = OnStragglerDied;
        }

        /// <summary>Raised when a Straggler comes in.</summary>
        public event Action<Enemy> StragglerSpawned;

        /// <summary>Stragglers alive right now.</summary>
        public int AliveCount { get; private set; }

        /// <summary>Seconds of walking until the next Straggler is due, or infinity when none will come.</summary>
        public float SecondsUntilNext => HasStragglers ? secondsUntilNext : float.PositiveInfinity;

        private bool HasStragglers => plan != null && plan.MaxAlive > 0;

        /// <summary>
        /// Switches to the Stragglers of the Room the character just entered (null for none) and starts a fresh interval.
        /// Stragglers already alive keep hunting.
        /// </summary>
        public void Follow(StragglerPlan<TEnemyType> roomStragglers)
        {
            plan = roomStragglers;
            RestartInterval();
        }

        /// <param name="deltaSeconds">Seconds since the last tick.</param>
        /// <param name="walking">Whether the character is walking between Stations. The clock stops otherwise.</param>
        public void Tick(float deltaSeconds, bool walking)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (!walking || !HasStragglers)
            {
                return;
            }

            secondsUntilNext -= deltaSeconds;
            if (secondsUntilNext > 0f)
            {
                return;
            }

            if (AliveCount >= plan.MaxAlive)
            {
                RestartInterval();
                return;
            }

            var straggler = spawner.TrySpawn(plan.EnemyType);
            if (straggler == null)
            {
                // Try again next tick; waiting for a dark Spawn point doesn't bank an earlier next Straggler.
                secondsUntilNext = 0f;
                return;
            }

            RestartInterval();
            if (straggler.IsAlive)
            {
                AliveCount++;
                straggler.Died += onStragglerDied;
            }

            StragglerSpawned?.Invoke(straggler);
        }

        private void RestartInterval()
        {
            secondsUntilNext = HasStragglers ? random.Range(plan.MinIntervalSeconds, plan.MaxIntervalSeconds) : 0f;
        }

        private void OnStragglerDied(Enemy straggler)
        {
            straggler.Died -= onStragglerDied;
            AliveCount--;
        }
    }
}
