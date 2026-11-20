#if UNITY_EDITOR || DEBUG
using System;
using DarknessRises.Game.Saving;
using DarknessRises.Game.Shop;
using UnityEngine;

namespace DarknessRises.Game.Debugging
{
    /// <summary>
    /// Debug only: reaches any state of the player's save from the DEV menu, so playtesters and headless checks don't
    /// have to earn it: add Credits, max out everything the Shop sells, or put the save back to a first install. Each
    /// command writes the save at once. Compiled out of release builds.
    /// </summary>
    /// <remarks>
    /// In a Level, what was maxed out shows up from the next Level started (the Run's stats are built when a Level
    /// starts), and a reset save boots straight into Level 1 the next time the app starts. In the Shop the screen
    /// follows the save as it changes.
    /// </remarks>
    public sealed class SaveDebugCommands
    {
        private readonly PlayerSave save;
        private readonly ShopDefinition shop;

        public SaveDebugCommands(PlayerSave playerSave, ShopDefinition shopDefinition)
        {
            save = playerSave ?? throw new ArgumentNullException(nameof(playerSave));
            shop = shopDefinition ? shopDefinition : throw new ArgumentNullException(nameof(shopDefinition));
        }

        /// <summary>How many Credits one press of Add Credits puts in the bank: authored on the Shop definition.</summary>
        public int CreditsPerPress => shop.DebugCredits;

        /// <summary>Puts <see cref="CreditsPerPress"/> Credits in the bank.</summary>
        public void AddCredits()
        {
            save.AddCredits(CreditsPerPress);
            save.Write();
            Debug.Log($"[Debug] +{CreditsPerPress} Credits: {save.Credits} in the bank.");
        }

        /// <summary>
        /// Raises everything the Shop sells to its top level, for free: every Weapon unlocked, every Upgrade maxed. The
        /// chosen Primary stays as it was.
        /// </summary>
        public void MaxOut()
        {
            var items = shop.ToCatalog().AllItems;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.MaxLevel > item.StartLevel && save.LevelOf(item.Id) < item.MaxLevel)
                {
                    save.Purchase(item.Id, item.MaxLevel, 0);
                }
            }

            save.Write();
            Debug.Log($"[Debug] Everything the Shop sells is maxed out ({items.Count} items).");
        }

        /// <summary>Puts the save back to a first install: no Credits, nothing bought, no Run finished.</summary>
        public void ResetSave()
        {
            save.Reset();
            save.Write();
            Debug.Log("[Debug] The save is back to a first install.");
        }
    }
}
#endif
