using System;
using DarknessRises.Game.Saving;

namespace DarknessRises.Game.Shop
{
    /// <summary>
    /// The Shop's rules: what the player has of each <see cref="ShopItem"/>, whether the next level can be bought, and
    /// buying it, which takes the Credits from the bank, raises the level and saves. A Weapon that is owned comes along
    /// on every Run from then on. Plain C# (ADR 0003) over the <see cref="PlayerSave"/>; buying is instant, with no
    /// confirmation.
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

        /// <summary>
        /// Raised when the Credits in the bank change, with the new total: something was bought, or Credits arrived from
        /// outside the Shop.
        /// </summary>
        public event Action<int> CreditsChanged
        {
            add => save.CreditsChanged += value;
            remove => save.CreditsChanged -= value;
        }

        /// <summary>
        /// Raised when anything the Shop shows may have changed: the bank or a level. Also when the save was changed
        /// from outside the Shop, as the dev tools do.
        /// </summary>
        public event Action Changed
        {
            add => save.Changed += value;
            remove => save.Changed -= value;
        }

        /// <summary>The Credits in the bank.</summary>
        public int Credits => save.Credits;

        /// <summary>The best Score of any Run. It buys nothing; the Shop only shows it.</summary>
        public int BestScore => save.BestScore;

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
        /// Whether the only thing between the player and the next level of <paramref name="item"/> is Credits: there is
        /// one, what it requires is owned, and the bank can't cover it.
        /// </summary>
        public bool IsShortOfCredits(ShopItem item)
        {
            return !IsMaxed(item) && !IsLocked(item) && save.Credits < NextPrice(item);
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

        private static void Require(ShopItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
        }
    }
}
