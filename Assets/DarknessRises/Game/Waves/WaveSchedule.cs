using System;
using System.Collections.Generic;
using DarknessRises.Game.Enemies;

namespace DarknessRises.Game.Waves
{
    /// <summary>
    /// The Waves of one Station: decides who spawns when, and is done once its last Wave is dead.
    /// Whoever runs the Station (see <see cref="StationFight{TEnemyType}"/>) begins it and clears the Station afterwards.
    /// Plain C#: time arrives as an explicit delta (ADR 0003).
    /// </summary>
    /// <remarks>
    /// Waves run one after another, each starting on a tick. Each Wave waits its delay (counted from the start of the fight for the first Wave, and from
    /// the previous Wave's last death for the rest), then lets its Enemies in one at a time, a spawn interval apart,
    /// the first one at once. When the spawner can't bring an Enemy in (every Spawn point is lit), that Enemy
    /// tries again every tick, and the time spent waiting is never banked as faster spawns later.
    /// Once every Enemy of the last Wave is dead, the schedule is done.
    /// </remarks>
    public sealed class WaveSchedule<TEnemyType>
    {
        private readonly WavePlan<TEnemyType>[] waves;
        private readonly IEnemySpawner<TEnemyType> spawner;
        private readonly Action<Enemy> onEnemyDied;

        private Phase phase;
        private float delaySecondsLeft;
        private float secondsUntilNextSpawn;
        private int spawnedInWave;

        public WaveSchedule(IReadOnlyList<WavePlan<TEnemyType>> waves, IEnemySpawner<TEnemyType> spawner)
        {
            if (waves == null)
            {
                throw new ArgumentNullException(nameof(waves));
            }

            this.spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));
            this.waves = new WavePlan<TEnemyType>[waves.Count];
            for (var i = 0; i < waves.Count; i++)
            {
                this.waves[i] = waves[i] ?? throw new ArgumentException("A Station's Waves can't be null.", nameof(waves));
            }

            onEnemyDied = OnEnemyDied;
            phase = Phase.NotStarted;
        }

        /// <summary>Raised when a Wave's delay is over and its first Enemy is due. Carries the Wave's index.</summary>
        public event Action<int> WaveStarted;

        private enum Phase
        {
            NotStarted,
            Waiting,
            Spawning,
            Fighting,
            Done,
        }

        /// <summary>Index of the Wave being waited for, spawned or fought. Equals the Wave count once the last Wave is dead.</summary>
        public int WaveIndex { get; private set; }

        public int WaveCount => waves.Length;

        /// <summary>Enemies of this Station that are alive right now.</summary>
        public int AliveCount { get; private set; }

        public bool IsRunning => phase != Phase.NotStarted && phase != Phase.Done;

        public bool IsDone => phase == Phase.Done;

        /// <summary>Whether <see cref="Begin"/> has been called.</summary>
        public bool HasBegun => phase != Phase.NotStarted;

        /// <summary>Starts the Waves. A Station with no Waves is done at once.</summary>
        public void Begin()
        {
            if (phase != Phase.NotStarted)
            {
                throw new InvalidOperationException("A Station's Waves run only once.");
            }

            WaveIndex = 0;
            if (waves.Length == 0)
            {
                Finish();
                return;
            }

            StartWaiting();
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (phase == Phase.Waiting)
            {
                delaySecondsLeft -= deltaSeconds;
                if (delaySecondsLeft > 0f)
                {
                    return;
                }

                StartSpawning();
            }
            else if (phase == Phase.Spawning)
            {
                secondsUntilNextSpawn -= deltaSeconds;
            }

            if (phase == Phase.Spawning)
            {
                SpawnDue();
            }

            if (phase == Phase.Fighting && AliveCount == 0)
            {
                NextWave();
            }
        }

        private void StartWaiting()
        {
            phase = Phase.Waiting;
            delaySecondsLeft = waves[WaveIndex].DelaySeconds;
        }

        private void StartSpawning()
        {
            phase = Phase.Spawning;
            spawnedInWave = 0;
            secondsUntilNextSpawn = 0f;
            WaveStarted?.Invoke(WaveIndex);
        }

        private void SpawnDue()
        {
            var wave = waves[WaveIndex];
            while (spawnedInWave < wave.Count && secondsUntilNextSpawn <= 0f)
            {
                var enemy = spawner.TrySpawn(wave.EnemyTypeAt(spawnedInWave));
                if (enemy == null)
                {
                    // Try again next tick; waiting for a dark Spawn point doesn't bank faster spawns.
                    secondsUntilNextSpawn = 0f;
                    return;
                }

                spawnedInWave++;
                secondsUntilNextSpawn += wave.SpawnIntervalSeconds;
                if (enemy.IsAlive)
                {
                    AliveCount++;
                    enemy.Died += onEnemyDied;
                }
            }

            if (spawnedInWave >= wave.Count)
            {
                phase = Phase.Fighting;
            }
        }

        private void NextWave()
        {
            WaveIndex++;
            if (WaveIndex >= waves.Length)
            {
                Finish();
                return;
            }

            StartWaiting();
        }

        private void Finish()
        {
            phase = Phase.Done;
        }

        private void OnEnemyDied(Enemy enemy)
        {
            enemy.Died -= onEnemyDied;
            AliveCount--;
        }
    }
}
