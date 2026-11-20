namespace DarknessRises.Game.Storage
{
    /// <summary>Where the browser's storage comes from: the one place that knows which platform the game runs on.</summary>
    public static class BrowserStorage
    {
        /// <summary>
        /// The browser's <c>localStorage</c> on the Web player. Null on every other platform and in the Editor, where
        /// the player's data stays in files and <c>PlayerPrefs</c>.
        /// </summary>
        public static IKeyValueStorage OfThisPlayer()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new BrowserLocalStorage();
#else
            return null;
#endif
        }

        /// <summary>
        /// On the Web player, puts the files written under the persistent data path into the browser, where they
        /// survive a reload of the page: until then they are only in memory. Nothing to do anywhere else.
        /// </summary>
        public static void SyncFiles()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            BrowserLocalStorage.SyncFiles();
#endif
        }
    }
}
