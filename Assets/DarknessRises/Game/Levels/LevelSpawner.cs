using System;
using System.Collections.Generic;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Waves;
using UnityEngine;

namespace DarknessRises.Game.Levels
{
    /// <summary>
    /// Brings a Level's Enemies in through the dark Spawn points of the Room the character is in, from the Enemy type's pool.
    /// Waves may use any dark Spawn point; Stragglers only dark ones ahead of the character.
    /// </summary>
    public sealed class LevelSpawner
    {
        private readonly EnemyPools pools;
        private readonly RouteProgress progress;
        private readonly Transform hunted;
        private readonly RoomSpawns[] rooms;

        /// <param name="roomSpawnPoints">Each Room's Spawn points, in Room order.</param>
        public LevelSpawner(
            EnemyPools pools,
            IReadOnlyList<RoomSpawnPoints> roomSpawnPoints,
            RouteProgress progress,
            Transform hunted,
            float litIntensity,
            float stragglerMinMetresAhead)
        {
            this.pools = pools ?? throw new ArgumentNullException(nameof(pools));
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
            this.hunted = hunted;

            var random = new System.Random();
            rooms = new RoomSpawns[roomSpawnPoints.Count];
            for (var i = 0; i < rooms.Length; i++)
            {
                rooms[i] = new RoomSpawns(roomSpawnPoints[i], litIntensity, stragglerMinMetresAhead, random);
            }

            Waves = new Spawner(this, forStragglers: false);
            Stragglers = new Spawner(this, forStragglers: true);
        }

        /// <summary>Brings Wave Enemies in through any dark Spawn point of the current Room.</summary>
        public IEnemySpawner<EnemyDefinition> Waves { get; }

        /// <summary>Brings Stragglers in through dark Spawn points of the current Room ahead of the character.</summary>
        public IEnemySpawner<EnemyDefinition> Stragglers { get; }

        /// <summary>The Room whose Spawn points are used.</summary>
        public int CurrentRoom { get; set; }

        /// <summary>Gathers the scene's lights again, so lights created since the last refresh count.</summary>
        public void RefreshLights()
        {
            rooms[CurrentRoom].Lighting.Refresh();
        }

        private Enemy TrySpawn(EnemyDefinition enemyType, bool forStragglers)
        {
            var room = rooms[CurrentRoom];
            ISpawnPointLighting closed = room.Lighting;
            if (forStragglers)
            {
                room.StragglerEntrances.Follow(progress.Position, progress.Direction);
                closed = room.StragglerEntrances;
            }

            if (!room.Picker.TryPick(closed, out var spawnPoint))
            {
                return null;
            }

            var enemy = pools.Take(enemyType);
            enemy.Spawn(room.Points.PositionOf(spawnPoint), hunted);
            return enemy.Enemy;
        }

        private sealed class RoomSpawns
        {
            public RoomSpawns(RoomSpawnPoints points, float litIntensity, float stragglerMinMetresAhead, System.Random random)
            {
                Points = points;
                Picker = new SpawnPointPicker(points.Count, random);
                Lighting = new SceneSpawnPointLighting(points, litIntensity);
                StragglerEntrances = new StragglerEntrances(Lighting, points, stragglerMinMetresAhead);
            }

            public RoomSpawnPoints Points { get; }
            public SpawnPointPicker Picker { get; }
            public SceneSpawnPointLighting Lighting { get; }
            public StragglerEntrances StragglerEntrances { get; }
        }

        private sealed class Spawner : IEnemySpawner<EnemyDefinition>
        {
            private readonly LevelSpawner owner;
            private readonly bool forStragglers;

            public Spawner(LevelSpawner owner, bool forStragglers)
            {
                this.owner = owner;
                this.forStragglers = forStragglers;
            }

            public Enemy TrySpawn(EnemyDefinition enemyType) => owner.TrySpawn(enemyType, forStragglers);
        }
    }
}
