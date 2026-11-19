using System;
using System.Collections.Generic;
using DarknessRises.Game.Content;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Runs;
using UnityEngine;

namespace DarknessRises.Game.Waves
{
    /// <summary>
    /// Runs the Stations' Waves in the scene: feeds frame time to <see cref="RouteWaves{TEnemyType}"/> and brings
    /// each Enemy it asks for in through a dark Spawn point of the Room, from that Enemy type's pool.
    /// </summary>
    public sealed class RouteWavesView : MonoBehaviour, IEnemySpawner<EnemyDefinition>
    {
        [SerializeField, Tooltip("The Room's Spawn points.")]
        private RoomSpawnPoints spawnPoints;

        private RouteWaves<EnemyDefinition> waves;
        private RouteProgress progress;
        private EnemyPools pools;
        private SpawnPointPicker picker;
        private SceneSpawnPointLighting lighting;
        private Transform hunted;

        /// <summary>Raised when any Enemy these Waves brought in dies.</summary>
        public event Action<Enemy> EnemyDied;

        /// <summary>Raised when any Enemy of these Waves comes in at a Spawn point.</summary>
        public event Action<EnemyView> EnemySpawned;

        /// <summary>
        /// Starts running each Station's authored Waves as the character reaches it, to the beats of
        /// <paramref name="rhythm"/>. Spawned Enemies run on <paramref name="enemyTime"/>, built from the prefabs and
        /// sounds already loaded in <paramref name="content"/>.
        /// </summary>
        public void Run(
            RouteDefinition route,
            RouteProgress routeProgress,
            Transform character,
            EnemyTime enemyTime,
            ILoadedContent content,
            StationRhythm rhythm)
        {
            Stop();

            progress = routeProgress;
            hunted = character;
            // The Test Room is a sandbox: its Waves and Enemies stay as authored, without the Ramp.
            pools = new EnemyPools(transform, enemyTime, content, RampStep.First);
            pools.EnemyDied += OnEnemyDied;
            pools.EnemySpawned += OnEnemySpawned;
            picker = new SpawnPointPicker(spawnPoints.Count, new System.Random());
            lighting = new SceneSpawnPointLighting(spawnPoints, route.SpawnPointLitIntensity);

            var stationWaves = new WavePlan<EnemyDefinition>[route.Stations.Count][];
            for (var i = 0; i < stationWaves.Length; i++)
            {
                stationWaves[i] = route.Stations[i].ToPlans();
            }

            PrewarmPools(stationWaves);
            progress.StationReached += OnStationReached;
            waves = new RouteWaves<EnemyDefinition>(progress, stationWaves, this, rhythm);
        }

        public Enemy TrySpawn(EnemyDefinition enemyType)
        {
            if (!picker.TryPick(lighting, out var spawnPoint))
            {
                return null;
            }

            var enemy = pools.Take(enemyType);
            enemy.Spawn(spawnPoints.PositionOf(spawnPoint), hunted);
            return enemy.Enemy;
        }

        private void Update()
        {
            waves?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            Stop();
        }

        private void Stop()
        {
            if (waves == null)
            {
                return;
            }

            progress.StationReached -= OnStationReached;
            waves.Dispose();
            pools.EnemyDied -= OnEnemyDied;
            pools.EnemySpawned -= OnEnemySpawned;
            pools.Dispose();
            waves = null;
        }

        private void OnEnemyDied(Enemy enemy)
        {
            EnemyDied?.Invoke(enemy);
        }

        private void OnEnemySpawned(EnemyView enemy)
        {
            EnemySpawned?.Invoke(enemy);
        }

        // Subscribed before the Waves are, so lights created since the last Station count from the first spawn.
        private void OnStationReached(Station _)
        {
            lighting.Refresh();
        }

        // Enough of each Enemy type for the Station that needs the most of it, so fights don't instantiate.
        private void PrewarmPools(IReadOnlyList<WavePlan<EnemyDefinition>[]> stationWaves)
        {
            var most = new Dictionary<EnemyDefinition, int>();
            var atStation = new Dictionary<EnemyDefinition, int>();
            foreach (var plans in stationWaves)
            {
                atStation.Clear();
                foreach (var plan in plans)
                {
                    for (var i = 0; i < plan.Count; i++)
                    {
                        var enemyType = plan.EnemyTypeAt(i);
                        atStation.TryGetValue(enemyType, out var count);
                        atStation[enemyType] = count + 1;
                    }
                }

                foreach (var pair in atStation)
                {
                    most.TryGetValue(pair.Key, out var count);
                    most[pair.Key] = Math.Max(count, pair.Value);
                }
            }

            foreach (var pair in most)
            {
                pools.Prewarm(pair.Key, pair.Value);
            }
        }
    }
}
