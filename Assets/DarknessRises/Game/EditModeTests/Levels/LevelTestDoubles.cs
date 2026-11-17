using System.Collections.Generic;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Randomness;
using DarknessRises.Game.Waves;

namespace DarknessRises.Game.EditModeTests.Levels
{
    /// <summary>Brings in one-hit Enemies on demand, or refuses while <see cref="Closed"/> (every Spawn point lit).</summary>
    internal sealed class FakeSpawner : IEnemySpawner<string>
    {
        public readonly List<Enemy> Alive = new();
        public readonly List<string> Spawned = new();

        public bool Closed { get; set; }

        public Enemy TrySpawn(string enemyType)
        {
            if (Closed)
            {
                return null;
            }

            var enemy = new Enemy();
            enemy.Spawn(1);
            Alive.Add(enemy);
            Spawned.Add(enemyType);
            return enemy;
        }

        public void KillAll()
        {
            foreach (var enemy in Alive.ToArray())
            {
                enemy.TakeHit(1);
            }

            Alive.Clear();
        }
    }

    /// <summary>Returns the queued values in order, then the top of each range asked for.</summary>
    internal sealed class ScriptedRandom : IRandom
    {
        private readonly Queue<float> values = new();

        public (float min, float max) LastRange { get; private set; }

        public void Next(params float[] next)
        {
            foreach (var value in next)
            {
                values.Enqueue(value);
            }
        }

        public float Range(float min, float max)
        {
            LastRange = (min, max);
            return values.Count > 0 ? values.Dequeue() : max;
        }
    }
}
