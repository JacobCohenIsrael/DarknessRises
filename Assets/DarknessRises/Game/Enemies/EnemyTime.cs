using System;

namespace DarknessRises.Game.Enemies
{
    /// <summary>
    /// How fast time runs for Enemies, apart from the global time scale: 1 is normal speed, lower is slower.
    /// Enemy rules and views scale their frame time, movement and sounds by it, so Adrenaline slows Enemies
    /// while the character, Shots and Skills keep full speed. Plain C# (ADR 0003), owned by the composition root.
    /// </summary>
    public sealed class EnemyTime
    {
        public const float NormalScale = 1f;

        private float scale = NormalScale;

        /// <summary>Raised with the new scale whenever it changes.</summary>
        public event Action<float> ScaleChanged;

        public float Scale
        {
            get => scale;
            set
            {
                if (value < 0f)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Enemy time can't run backwards.");
                }

                if (value == scale)
                {
                    return;
                }

                scale = value;
                ScaleChanged?.Invoke(scale);
            }
        }

        /// <summary><paramref name="deltaSeconds"/> of game time, as Enemies experience it.</summary>
        public float Scaled(float deltaSeconds)
        {
            return deltaSeconds * scale;
        }
    }
}
