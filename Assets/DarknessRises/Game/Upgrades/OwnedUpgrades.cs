using System;
using System.Collections.Generic;
using DarknessRises.Game.Shop;

namespace DarknessRises.Game.Upgrades
{
    /// <summary>
    /// The Upgrades of one owner (a Primary weapon, the Grenade, the Skills, the character) at the levels the player
    /// has bought: given a number's authored value, it answers what the number is for the next Run.
    /// Plain C# (ADR 0003).
    /// </summary>
    public sealed class OwnedUpgrades
    {
        /// <summary>No Upgrades: every number stays as authored.</summary>
        public static readonly OwnedUpgrades None = new(Array.Empty<Upgrade>(), _ => 0);

        private readonly IReadOnlyList<Upgrade> upgrades;
        private readonly Func<ShopItem, int> levelOf;

        /// <param name="upgrades">The owner's Upgrades. At most one per stat counts: the first.</param>
        /// <param name="levelOf">The level the player has of an item: the Shop's rules.</param>
        public OwnedUpgrades(IReadOnlyList<Upgrade> upgrades, Func<ShopItem, int> levelOf)
        {
            this.upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            this.levelOf = levelOf ?? throw new ArgumentNullException(nameof(levelOf));
        }

        /// <summary><paramref name="baseValue"/> as the owner's Upgrade of <paramref name="stat"/> sets it, or unchanged without one.</summary>
        public float Value(UpgradeStat stat, float baseValue)
        {
            for (var i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i].Stat == stat)
                {
                    return upgrades[i].ValueAt(levelOf(upgrades[i].Item), baseValue);
                }
            }

            return baseValue;
        }

        /// <summary>A whole number (a magazine, charges, hit points, damage) as its Upgrade sets it, rounded.</summary>
        public int Whole(UpgradeStat stat, int baseValue)
        {
            return (int)Math.Round(Value(stat, baseValue), MidpointRounding.AwayFromZero);
        }
    }
}
