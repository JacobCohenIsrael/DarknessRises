using System;
using DarknessRises.Game.Storage;

namespace DarknessRises.Game.Saving
{
    /// <summary>Picks where the save is kept on the platform the game runs on.</summary>
    public static class SaveStorages
    {
        /// <summary>
        /// The save of this player: the file in the app's persistent data path on a phone, a desktop and in the Editor;
        /// the browser's storage, with that file to fall back on, on the Web player.
        /// </summary>
        public static ISaveStorage OfThisPlayer()
        {
            return Choose(BrowserStorage.OfThisPlayer(), FileSaveStorage.InPersistentData(), BrowserStorage.SyncFiles);
        }

        /// <summary>
        /// With no browser storage (every platform but the Web) the save is the file, untouched. With it, the save is
        /// kept under <see cref="BrowserKeys.Save"/> first and falls back to the file: the Web player's files are kept
        /// under a path made from the page's address, so they don't follow the game to a new upload, while the
        /// browser's storage does. Those files also stay in memory until <paramref name="syncFiles"/> puts them in
        /// the browser, so a save that does fall back to the file asks for that each time it's written.
        /// </summary>
        public static ISaveStorage Choose(IKeyValueStorage browser, ISaveStorage file, Action syncFiles)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (browser == null)
            {
                return file;
            }

            if (syncFiles == null)
            {
                throw new ArgumentNullException(nameof(syncFiles));
            }

            return new FallbackSaveStorage(new KeyedSaveStorage(browser, BrowserKeys.Save), new SyncedSaveStorage(file, syncFiles));
        }
    }
}
