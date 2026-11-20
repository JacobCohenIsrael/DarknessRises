using System;
using System.Collections.Generic;

namespace DarknessRises.Game.Shop
{
    /// <summary>
    /// One thing the Shop sells: a ladder of levels, each bought with Credits, one at a time. A Weapon to unlock has one
    /// level (owned or not); an Upgrade has several, each dearer than the last. An item can start above level 0, as the
    /// Weapon the player owns from the start does, and it can require another item, as a Weapon's Upgrades require the
    /// Weapon. Plain C# (ADR 0003), copied out of the authored Shop definition.
    /// </summary>
    public sealed class ShopItem
    {
        private readonly int[] prices;

        /// <param name="id">The id the item is saved under. Stable: players' saves hold it.</param>
        /// <param name="prices">The price of each level above the starting level, in order.</param>
        /// <param name="startLevel">The level the player has without buying anything.</param>
        /// <param name="requires">An item that must be owned before this one can be bought: the Weapon an Upgrade belongs to.</param>
        public ShopItem(string id, IReadOnlyList<int> prices, int startLevel = 0, ShopItem requires = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("A Shop item needs an id to be saved under.", nameof(id));
            }

            if (prices == null)
            {
                throw new ArgumentNullException(nameof(prices));
            }

            if (startLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startLevel), startLevel, "Levels count from 0.");
            }

            this.prices = new int[prices.Count];
            for (var i = 0; i < prices.Count; i++)
            {
                if (prices[i] < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(prices), prices[i], "Nothing in the Shop pays the player.");
                }

                this.prices[i] = prices[i];
            }

            Id = id;
            StartLevel = startLevel;
            Requires = requires;
        }

        public string Id { get; }

        /// <summary>The level the player has without buying anything.</summary>
        public int StartLevel { get; }

        /// <summary>The item that must be owned before this one can be bought, or null when it needs none.</summary>
        public ShopItem Requires { get; }

        /// <summary>The highest level there is to buy.</summary>
        public int MaxLevel => StartLevel + prices.Length;

        /// <summary>What buying <paramref name="level"/> costs, coming from the level below it.</summary>
        public int PriceOf(int level)
        {
            if (level <= StartLevel || level > MaxLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, $"'{Id}' sells levels {StartLevel + 1} to {MaxLevel}.");
            }

            return prices[level - StartLevel - 1];
        }
    }
}
