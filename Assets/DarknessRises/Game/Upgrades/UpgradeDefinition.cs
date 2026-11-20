using System;
using System.Collections.Generic;
using DarknessRises.Game.Shop;
using UnityEngine;

namespace DarknessRises.Game.Upgrades
{
    /// <summary>One level of an Upgrade: what it costs, and the value it sets the number to.</summary>
    [Serializable]
    public sealed class UpgradeLevel
    {
        [SerializeField, Min(0), Tooltip("Credits this level costs. Prices should rise from one level to the next.")]
        private int price;

        [SerializeField, Tooltip("The value this level sets the number to (not an amount added): a magazine of 15, a reload of 0.85 seconds.")]
        private float value;

        public int Price => price;
        public float Value => value;
    }

    /// <summary>
    /// An Upgrade as the Shop sells it: which number it sets, its name on the Shop's row, how the number reads, and its
    /// levels. Without a level the number is the one authored on its own definition (the Weapon, the Grenade, the Skill,
    /// the character), so that stays the single place the starting value lives.
    /// </summary>
    [Serializable]
    public sealed class UpgradeDefinition
    {
        [SerializeField, Tooltip(
            "The id this Upgrade is saved under in the player's save. Never change it once players have it: they would lose its levels.")]
        private string id = string.Empty;

        [SerializeField, Tooltip("The number it sets. In a Weapon's list it's that Weapon's number.")]
        private UpgradeStat stat;

        [SerializeField, Tooltip("Its name on the Shop's row.")]
        private string displayName = string.Empty;

        [SerializeField, Tooltip(
            "How the number reads on the row. {0} is the value: \"{0}\" prints a whole number, \"{0:1} s\" one decimal and a unit.")]
        private string valueFormat = "{0}";

        [SerializeField, Tooltip("The levels, in order. Each has a price and the value it sets.")]
        private UpgradeLevel[] levels = Array.Empty<UpgradeLevel>();

        public string Id => id;
        public UpgradeStat Stat => stat;
        public string DisplayName => displayName;
        public string ValueFormat => valueFormat;
        public IReadOnlyList<UpgradeLevel> Levels => levels;

        /// <summary>
        /// The Upgrade as the rules see it. With <paramref name="requires"/>, it can only be bought once that item is
        /// owned: a Weapon's Upgrades need the Weapon.
        /// </summary>
        public Upgrade ToUpgrade(ShopItem requires = null)
        {
            var prices = new int[levels.Length];
            var values = new float[levels.Length];
            for (var i = 0; i < levels.Length; i++)
            {
                prices[i] = levels[i].Price;
                values[i] = levels[i].Value;
            }

            return new Upgrade(new ShopItem(id, prices, startLevel: 0, requires), stat, values);
        }
    }
}
