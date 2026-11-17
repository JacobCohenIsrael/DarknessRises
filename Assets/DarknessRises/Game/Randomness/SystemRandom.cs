using System;

namespace DarknessRises.Game.Randomness
{
    /// <summary>
    /// <see cref="IRandom"/> backed by <see cref="System.Random"/>. It doesn't touch Unity's global random state.
    /// </summary>
    public sealed class SystemRandom : IRandom
    {
        private readonly Random random;

        public SystemRandom()
        {
            random = new Random();
        }

        public SystemRandom(int seed)
        {
            random = new Random(seed);
        }

        public float Range(float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
