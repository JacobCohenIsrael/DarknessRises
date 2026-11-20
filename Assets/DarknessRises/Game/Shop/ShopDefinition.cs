using System;
using System.Collections.Generic;
using DarknessRises.Game.Upgrades;
using DarknessRises.Game.Weapons;
using UnityEngine;

namespace DarknessRises.Game.Shop
{
    /// <summary>
    /// Authored catalog of the Shop: the Primary weapons it sells with their prices and Upgrades, the Upgrades of the
    /// Grenade, the Skills and the character, and how its screen reads, looks and sounds. The Shop is open only between
    /// Runs. Never changed at runtime: what the player has bought lives in their save, and an Upgrade's starting value
    /// lives on the definition it upgrades.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Shop Definition", fileName = "ShopDefinition", order = 96)]
    public sealed class ShopDefinition : ScriptableObject
    {
        [SerializeField, Tooltip(
            "The Primary weapons, in the order the Shop shows them, each with its Upgrades. One Primary per Run: the player unlocks them " +
            "here and chooses which one the character carries. At least one must cost nothing, so the player owns a Primary from the start.")]
        private WeaponOffer[] weapons = Array.Empty<WeaponOffer>();

        [SerializeField, Tooltip("The Grenade's Upgrades: charges, refill time, blast radius.")]
        private UpgradeDefinition[] grenadeUpgrades = Array.Empty<UpgradeDefinition>();

        [SerializeField, Tooltip("The Skills' Upgrades: the Flare's and Slow-mo's duration and cooldown.")]
        private UpgradeDefinition[] skillUpgrades = Array.Empty<UpgradeDefinition>();

        [SerializeField, Tooltip(
            "The character's Upgrades: hit points and the Aura's reach. Keep the Aura's levels small: Darkness has to stay dark.")]
        private UpgradeDefinition[] characterUpgrades = Array.Empty<UpgradeDefinition>();

        [SerializeField, Tooltip("The Shop screen's wording, colours, and the pop and sound of a purchase.")]
        private ShopScreenSettings screen = new();

        public IReadOnlyList<WeaponOffer> Weapons => weapons;
        public ShopScreenSettings Screen => screen;

        /// <summary>Everything the Shop sells, as the rules see it. Make it once per use: its items are new objects each time.</summary>
        public ShopCatalog ToCatalog()
        {
            var weaponEntries = new ShopCatalog.WeaponEntry[weapons.Length];
            for (var i = 0; i < weaponEntries.Length; i++)
            {
                // A Weapon's Upgrades can't be bought before the Weapon is.
                var item = weapons[i].ToItem();
                weaponEntries[i] = new ShopCatalog.WeaponEntry(weapons[i], item, ToEntries(weapons[i].Upgrades, item));
            }

            return new ShopCatalog(
                weaponEntries, ToEntries(grenadeUpgrades, null), ToEntries(skillUpgrades, null), ToEntries(characterUpgrades, null));
        }

        /// <summary>The Weapons as the Shop's rules see them, in the same order as <see cref="Weapons"/>.</summary>
        public ShopItem[] ToWeaponItems()
        {
            var items = new ShopItem[weapons.Length];
            for (var i = 0; i < items.Length; i++)
            {
                items[i] = weapons[i].ToItem();
            }

            return items;
        }

        /// <summary>
        /// The Primary weapon the character carries into the next Run, as <paramref name="keeper"/> has it: the one the
        /// player chose, or the one owned from the start.
        /// </summary>
        public WeaponDefinition PrimaryWeapon(ShopKeeper keeper)
        {
            var items = ToWeaponItems();
            return weapons[Array.IndexOf(items, keeper.PrimaryOf(items))].Weapon;
        }

        /// <summary>Every Primary weapon there is, in the Shop's order.</summary>
        public WeaponDefinition[] ToPrimaryWeapons()
        {
            var definitions = new WeaponDefinition[weapons.Length];
            for (var i = 0; i < definitions.Length; i++)
            {
                definitions[i] = weapons[i].Weapon;
            }

            return definitions;
        }

        private static ShopCatalog.UpgradeEntry[] ToEntries(IReadOnlyList<UpgradeDefinition> upgrades, ShopItem requires)
        {
            var entries = new ShopCatalog.UpgradeEntry[upgrades.Count];
            for (var i = 0; i < entries.Length; i++)
            {
                entries[i] = new ShopCatalog.UpgradeEntry(upgrades[i], upgrades[i].ToUpgrade(requires));
            }

            return entries;
        }
    }
}
