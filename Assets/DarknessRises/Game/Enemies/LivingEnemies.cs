using System.Collections.Generic;
using UnityEngine;

namespace DarknessRises.Game.Enemies
{
    /// <summary>
    /// The scene's living Enemies, as they spawn, for whoever needs to know how near the nearest one is. Dead and pooled
    /// Enemies drop out by themselves. Owned by the scene's composition root, which adds each Enemy as it spawns.
    /// Never global.
    /// </summary>
    public sealed class LivingEnemies
    {
        private const int InitialCapacity = 32;

        private readonly List<EnemyView> enemies = new(InitialCapacity);

        /// <summary>How many Enemies are listed. Some may have died or gone back to their pool since: check each one.</summary>
        public int Count => enemies.Count;

        /// <summary>A listed Enemy, which may have died or gone back to its pool since it was added.</summary>
        public EnemyView this[int index] => enemies[index];

        /// <summary>An Enemy has spawned.</summary>
        public void Add(EnemyView enemy)
        {
            if (!enemies.Contains(enemy))
            {
                enemies.Add(enemy);
            }
        }

        /// <summary>
        /// Metres across the floor from <paramref name="point"/> to the nearest living Enemy, or infinity when there's none.
        /// </summary>
        public float NearestMetres(Vector3 point)
        {
            var nearest = float.PositiveInfinity;
            for (var i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.Enemy.IsAlive || !enemy.isActiveAndEnabled)
                {
                    enemies.RemoveAt(i);
                    continue;
                }

                var offset = enemy.transform.position - point;
                offset.y = 0f;
                nearest = Mathf.Min(nearest, offset.sqrMagnitude);
            }

            return float.IsPositiveInfinity(nearest) ? nearest : Mathf.Sqrt(nearest);
        }
    }
}
