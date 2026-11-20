using System;
using System.Collections.Generic;
using DarknessRises.Game.Saving;

namespace DarknessRises.Game.Shop
{
    /// <summary>
    /// The Shop's rules: what the player has of each <see cref="ShopItem"/>, whether the next level can be bought, and
    /// buying it, which takes the Credits from the bank, raises the level and saves. It also keeps the choice of the
    /// Primary weapon the character carries into the next Run: one of the Weapons the player owns.
    /// Plain C# (ADR 0003) over the <see cref="PlayerSave"/>; buying is instant, with no confirmation.
    /// </summary>
    public sealed class ShopKeeper
    {
        private readonly PlayerSave save;

        public ShopKeeper(PlayerSave playerSave)
        {
            save = playerSave ?? throw new ArgumentNullException(nameof(playerSave));
        }

        /// <summary>Raised after an item's next level was bought.</summary>
        public event Action<ShopItem> Bought;

        /// <summary>Raised after a different Primary weapon was chosen.</summary>
        public event Action<ShopItem> PrimaryChosen;

        /// <summary>
        /// Raised when the Credits in the bank change, with the new total: something was bought, or Credits arrived from
        /// outside the Shop.
        /// </summary>
        public event Action<int> CreditsChanged
        {
            add => save.CreditsChanged += value;
            remove => save.CreditsChanged -= value;
        }

        /// <summary>The Credits in the bank.</summary>
        public int Credits => save.Credits;

        /// <summary>The level the player has of <paramref name="item"/>: never below where it starts or above its top.</summary>
        public int LevelOf(ShopItem item)
        {
            Require(item);
            return Math.Min(item.MaxLevel, Math.Max(item.StartLevel, save.LevelOf(item.Id)));
        }

        /// <summary>Whether the player has <paramref name="item"/> at all: a Weapon that's unlocked, an Upgrade with a level.</summary>
        public bool Owns(ShopItem item) => LevelOf(item) > 0;

        /// <summary>
        /// Whether <paramref name="item"/> can't be bought yet because what it requires isn't owned: a Weapon's Upgrade
        /// while the Weapon is still to unlock.
        /// </summary>
        public bool IsLocked(ShopItem item)
        {
            Require(item);
            return item.Requires != null && !Owns(item.Requires);
        }

        /// <summary>Whether there is nothing more of <paramref name="item"/> to buy.</summary>
        public bool IsMaxed(ShopItem item) => LevelOf(item) >= item.MaxLevel;

        /// <summary>What the next level of <paramref name="item"/> costs. 0 when it's maxed.</summary>
        public int NextPrice(ShopItem item)
        {
            return IsMaxed(item) ? 0 : item.PriceOf(LevelOf(item) + 1);
        }

        /// <summary>
        /// Whether the next level of <paramref name="item"/> can be bought now: there is one, what it requires is
        /// owned, and the bank covers it.
        /// </summary>
        public bool CanBuy(ShopItem item)
        {
            return !IsMaxed(item) && !IsLocked(item) && save.Credits >= NextPrice(item);
        }

        /// <summary>
        /// Buys the next level of <paramref name="item"/>: takes its price from the bank, raises the level by one and
        /// writes the save. Does nothing when it's maxed, locked or the bank can't cover it.
        /// </summary>
        /// <returns>Whether it was bought.</returns>
        public bool Buy(ShopItem item)
        {
            if (!CanBuy(item))
            {
                return false;
            }

            var level = LevelOf(item) + 1;
            if (!save.Purchase(item.Id, level, item.PriceOf(level)))
            {
                return false;
            }

            save.Write();
            Bought?.Invoke(item);
            return true;
        }

        /// <summary>
        /// The Primary weapon the character carries into the next Run, out of <paramref name="weapons"/>: the one the
        /// player chose if they own it, otherwise the first one they own (the starting Weapon). One Primary per Run.
        /// </summary>
        public ShopItem PrimaryOf(IReadOnlyList<ShopItem> weapons)
        {
            if (weapons == null)
            {
                throw new ArgumentNullException(nameof(weapons));
            }

            ShopItem firstOwned = null;
            for (var i = 0; i < weapons.Count; i++)
            {
                var weapon = weapons[i];
                if (!Owns(weapon))
                {
                    continue;
                }

                if (weapon.Id == save.PrimaryWeaponId)
                {
                    return weapon;
                }

                firstOwned ??= weapon;
            }

            return firstOwned ?? throw new InvalidOperationException("The player owns no Primary weapon: one must be owned from the start.");
        }

        /// <summary>
        /// Chooses <paramref name="weapon"/> as the Primary for the next Run and writes the save. Only a Weapon the
        /// player owns can be chosen.
        /// </summary>
        /// <returns>Whether the choice changed.</returns>
        public bool ChoosePrimary(ShopItem weapon)
        {
            if (!Owns(weapon) || !save.ChoosePrimaryWeapon(weapon.Id))
            {
                return false;
            }

            save.Write();
            PrimaryChosen?.Invoke(weapon);
            return true;
        }

        private static void Require(ShopItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
        }
    }
}
