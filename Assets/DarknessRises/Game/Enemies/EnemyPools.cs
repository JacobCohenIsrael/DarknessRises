using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace DarknessRises.Game.Enemies
{
    /// <summary>
    /// One pool of Enemies per Enemy type. Enemies are created ahead of time and come back to their pool
    /// once they have sunk away, so a fight doesn't instantiate or destroy anything.
    /// </summary>
    public sealed class EnemyPools : IDisposable
    {
        private readonly Transform root;
        private readonly Transform hatchery;
        private readonly Dictionary<EnemyDefinition, ObjectPool<EnemyView>> pools = new();

        public EnemyPools(Transform parent)
        {
            root = new GameObject("Enemies").transform;
            root.SetParent(parent, false);

            // Enemies are instantiated under this inactive object, so they never wake up before they are spawned.
            hatchery = new GameObject("Hatchery").transform;
            hatchery.SetParent(root, false);
            hatchery.gameObject.SetActive(false);
        }

        /// <summary>Makes sure at least <paramref name="count"/> Enemies of the type are ready in its pool.</summary>
        public void Prewarm(EnemyDefinition definition, int count)
        {
            var pool = PoolFor(definition);
            var missing = count - pool.CountInactive;
            if (missing <= 0)
            {
                return;
            }

            var warmed = new EnemyView[missing];
            for (var i = 0; i < missing; i++)
            {
                warmed[i] = pool.Get();
            }

            for (var i = 0; i < missing; i++)
            {
                pool.Release(warmed[i]);
            }
        }

        /// <summary>
        /// An inactive Enemy of the type, taken from its pool. It wakes up when it's spawned with
        /// <see cref="EnemyView.Spawn"/>, and goes back to the pool by itself once it has sunk away.
        /// </summary>
        public EnemyView Take(EnemyDefinition definition)
        {
            return PoolFor(definition).Get();
        }

        public void Dispose()
        {
            foreach (var pool in pools.Values)
            {
                pool.Dispose();
            }

            pools.Clear();

            // On scene unload the Enemies may already be gone.
            if (root != null)
            {
                Object.Destroy(root.gameObject);
            }
        }

        private ObjectPool<EnemyView> PoolFor(EnemyDefinition definition)
        {
            if (pools.TryGetValue(definition, out var pool))
            {
                return pool;
            }

            ObjectPool<EnemyView> created = null;
            created = new ObjectPool<EnemyView>(
                () => Create(definition, created),
                actionOnGet: null,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroy);
            pools.Add(definition, created);
            return created;
        }

        private EnemyView Create(EnemyDefinition definition, IObjectPool<EnemyView> pool)
        {
            var enemy = Object.Instantiate(definition.Prefab, hatchery);
            enemy.name = definition.name;
            enemy.gameObject.SetActive(false);
            enemy.transform.SetParent(root, false);
            enemy.Prepare(pool, definition);
            return enemy;
        }

        private static void OnRelease(EnemyView enemy)
        {
            enemy.gameObject.SetActive(false);
        }

        private static void OnDestroy(EnemyView enemy)
        {
            if (enemy != null)
            {
                Object.Destroy(enemy.gameObject);
            }
        }
    }
}
