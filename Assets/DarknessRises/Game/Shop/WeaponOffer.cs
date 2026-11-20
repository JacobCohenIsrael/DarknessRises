using System;
using DarknessRises.Game.Weapons;
using UnityEngine;

namespace DarknessRises.Game.Shop
{
    /// <summary>
    /// A Primary weapon as the Shop sells it: its name and pitch on the card, what unlocking it costs, and the Weapon
    /// the character carries into a Run when it's the chosen one.
    /// </summary>
    [Serializable]
    public sealed class WeaponOffer
    {
        [SerializeField, Tooltip(
            "The id this Weapon is saved under in the player's save. Never change it once players have it: they would lose the Weapon.")]
        private string id = string.Empty;

        [SerializeField, Tooltip("Its name on the Shop's card.")]
        private string displayName = string.Empty;

        [SerializeField, Tooltip("One short line under the name: what it's like to fire.")]
        private string description = string.Empty;

        [SerializeField, Tooltip("The Weapon itself.")]
        private WeaponDefinition weapon;

        [SerializeField, Min(0), Tooltip("Credits to unlock it. 0: the player owns it from the start.")]
        private int price;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public WeaponDefinition Weapon => weapon;
        public int Price => price;

        /// <summary>Whether the player owns it without buying it.</summary>
        public bool IsOwnedFromTheStart => price <= 0;

        /// <summary>The offer as the Shop's rules see it: one level to buy, or owned from the start.</summary>
        public ShopItem ToItem()
        {
            return IsOwnedFromTheStart
                ? new ShopItem(id, Array.Empty<int>(), startLevel: 1)
                : new ShopItem(id, new[] { price });
        }
    }
}
