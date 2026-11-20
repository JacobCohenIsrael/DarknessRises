using System;
using System.Collections.Generic;
using DarknessRises.Game.Shop;

namespace DarknessRises.Game.Upgrades
{
    /// <summary>
    /// An Upgrade as the rules see it: a <see cref="ShopItem"/> to buy level by level, the number it sets
    /// (<see cref="UpgradeStat"/>), and the value each level sets it to. Without a level the number stays as its
    /// definition authored it. Plain C# (ADR 0003), copied out of the authored Shop definition: buying an Upgrade never
    /// changes a definition, it changes what a Run's stats are built from.
    /// </summary>
    public sealed class Upgrade
    {
        private readonly float[] values;

        /// <param name="item">What is bought: one level for each value.</param>
        /// <param name="stat">The number it sets.</param>
        /// <param name="values">The value each level sets, in order, from level 1.</param>
        public Upgrade(ShopItem item, UpgradeStat stat, IReadOnlyList<float> values)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (item.StartLevel != 0 || item.MaxLevel != values.Count)
            {
                throw new ArgumentException($"'{item.Id}' sells {item.MaxLevel - item.StartLevel} levels but sets {values.Count} values.", nameof(values));
            }

            this.values = new float[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                this.values[i] = values[i];
            }

            Stat = stat;
        }

        public ShopItem Item { get; }

        public UpgradeStat Stat { get; }

        public int MaxLevel => values.Length;

        /// <summary>
        /// The number at <paramref name="level"/>: <paramref name="baseValue"/> (the definition's own) without a level,
        /// otherwise what that level sets. A level above the top counts as the top.
        /// </summary>
        public float ValueAt(int level, float baseValue)
        {
            if (level <= 0 || values.Length == 0)
            {
                return baseValue;
            }

            return values[Math.Min(level, values.Length) - 1];
        }
    }
}
