using System;

namespace DarknessRises.Game.Waves
{
    /// <summary>
    /// Chooses which of a Room's Spawn points the next Enemy enters through. A lit Spawn point is skipped,
    /// so Enemies never appear in view. Among the dark ones it picks at random, avoiding the last one used
    /// when another is dark, so Enemies don't stack up in one doorway. Picking never allocates.
    /// </summary>
    public sealed class SpawnPointPicker
    {
        private const int None = -1;

        private readonly Random random;
        private readonly bool[] dark;
        private int lastPicked = None;

        public SpawnPointPicker(int spawnPointCount, Random random)
        {
            if (spawnPointCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(spawnPointCount), spawnPointCount, "A Room can't have fewer than no Spawn points.");
            }

            this.random = random ?? throw new ArgumentNullException(nameof(random));
            dark = new bool[spawnPointCount];
        }

        public int SpawnPointCount => dark.Length;

        /// <summary>Picks a dark Spawn point. Returns false when every Spawn point is lit (or the Room has none).</summary>
        public bool TryPick(ISpawnPointLighting lighting, out int spawnPoint)
        {
            if (lighting == null)
            {
                throw new ArgumentNullException(nameof(lighting));
            }

            var darkCount = 0;
            for (var i = 0; i < dark.Length; i++)
            {
                dark[i] = !lighting.IsLit(i);
                if (dark[i])
                {
                    darkCount++;
                }
            }

            if (darkCount == 0)
            {
                spawnPoint = None;
                return false;
            }

            if (darkCount > 1 && lastPicked != None && dark[lastPicked])
            {
                dark[lastPicked] = false;
                darkCount--;
            }

            var choice = random.Next(darkCount);
            for (var i = 0; i < dark.Length; i++)
            {
                if (!dark[i])
                {
                    continue;
                }

                if (choice == 0)
                {
                    lastPicked = i;
                    spawnPoint = i;
                    return true;
                }

                choice--;
            }

            throw new InvalidOperationException("Unreachable: a dark Spawn point was counted but not found.");
        }
    }
}
