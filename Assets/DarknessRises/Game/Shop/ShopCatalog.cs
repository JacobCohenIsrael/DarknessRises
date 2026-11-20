using System;
using System.Collections.Generic;
using DarknessRises.Game.Upgrades;

namespace DarknessRises.Game.Shop
{
    /// <summary>
    /// Everything the Shop sells, as the rules see it, next to how it was authored: each Primary weapon with its unlock
    /// and its Upgrades, and the Upgrades of the Grenade, the Skills and the character. Made once from the
    /// <see cref="ShopDefinition"/> by whoever needs it (the Shop screen, a Run building its stats), so items are the
    /// same objects everywhere it's used.
    /// </summary>
    public sealed class ShopCatalog
    {
        private readonly WeaponEntry[] weapons;
        private readonly ShopItem[] weaponItems;
        private readonly List<ShopItem> allItems = new();

        public ShopCatalog(
            IReadOnlyList<WeaponEntry> weapons,
            IReadOnlyList<UpgradeEntry> grenade,
            IReadOnlyList<UpgradeEntry> skills,
            IReadOnlyList<UpgradeEntry> character)
        {
            if (weapons == null)
            {
                throw new ArgumentNullException(nameof(weapons));
            }

            this.weapons = new WeaponEntry[weapons.Count];
            weaponItems = new ShopItem[weapons.Count];
            for (var i = 0; i < weapons.Count; i++)
            {
                this.weapons[i] = weapons[i];
                weaponItems[i] = weapons[i].Item;
                allItems.Add(weapons[i].Item);
                Add(weapons[i].Upgrades);
            }

            Grenade = grenade ?? throw new ArgumentNullException(nameof(grenade));
            Skills = skills ?? throw new ArgumentNullException(nameof(skills));
            Character = character ?? throw new ArgumentNullException(nameof(character));
            Add(grenade);
            Add(skills);
            Add(character);
        }

        /// <summary>The Primary weapons, in the Shop's order.</summary>
        public IReadOnlyList<WeaponEntry> Weapons => weapons;

        /// <summary>The Primary weapons' unlocks, in the same order.</summary>
        public IReadOnlyList<ShopItem> WeaponItems => weaponItems;

        public IReadOnlyList<UpgradeEntry> Grenade { get; }
        public IReadOnlyList<UpgradeEntry> Skills { get; }
        public IReadOnlyList<UpgradeEntry> Character { get; }

        /// <summary>Every item there is to buy: the Weapons and every Upgrade.</summary>
        public IReadOnlyList<ShopItem> AllItems => allItems;

        /// <summary>The Upgrades in <paramref name="entries"/>, as the rules see them.</summary>
        public static Upgrade[] UpgradesOf(IReadOnlyList<UpgradeEntry> entries)
        {
            var upgrades = new Upgrade[entries.Count];
            for (var i = 0; i < upgrades.Length; i++)
            {
                upgrades[i] = entries[i].Upgrade;
            }

            return upgrades;
        }

        private void Add(IReadOnlyList<UpgradeEntry> entries)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                allItems.Add(entries[i].Upgrade.Item);
            }
        }

        /// <summary>One Upgrade: as authored, and as the rules see it.</summary>
        public sealed class UpgradeEntry
        {
            public UpgradeEntry(UpgradeDefinition definition, Upgrade upgrade)
            {
                Definition = definition ?? throw new ArgumentNullException(nameof(definition));
                Upgrade = upgrade ?? throw new ArgumentNullException(nameof(upgrade));
            }

            public UpgradeDefinition Definition { get; }
            public Upgrade Upgrade { get; }
        }

        /// <summary>One Primary weapon: its offer as authored, its unlock, and its Upgrades (which need the Weapon).</summary>
        public sealed class WeaponEntry
        {
            public WeaponEntry(WeaponOffer offer, ShopItem item, IReadOnlyList<UpgradeEntry> upgrades)
            {
                Offer = offer ?? throw new ArgumentNullException(nameof(offer));
                Item = item ?? throw new ArgumentNullException(nameof(item));
                Upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            }

            public WeaponOffer Offer { get; }
            public ShopItem Item { get; }
            public IReadOnlyList<UpgradeEntry> Upgrades { get; }
        }
    }
}
