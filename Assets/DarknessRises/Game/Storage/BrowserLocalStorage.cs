#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;

namespace DarknessRises.Game.Storage
{
    /// <summary>
    /// The browser's <c>localStorage</c>, on the Web player only. A thin adapter over <c>BrowserLocalStorage.jslib</c>:
    /// it reads, writes and removes text under a key, and answers false when the browser blocks storage or it's full.
    /// Unlike the player's files (kept in IndexedDB under a path made from the page's URL), it belongs to the page's
    /// origin, so a new upload of the game served from a new URL still finds it.
    /// </summary>
    public sealed class BrowserLocalStorage : IKeyValueStorage
    {
        private const int Stored = 1;

        [DllImport("__Internal")]
        private static extern string DarknessRisesStorageRead(string key);

        [DllImport("__Internal")]
        private static extern int DarknessRisesStorageWrite(string key, string value);

        [DllImport("__Internal")]
        private static extern void DarknessRisesStorageRemove(string key);

        [DllImport("__Internal")]
        private static extern void DarknessRisesStorageSyncFiles();

        /// <summary>
        /// Puts the player's files into the browser's IndexedDB. On the Web player a file written under the persistent
        /// data path stays in memory until this is called.
        /// </summary>
        public static void SyncFiles() => DarknessRisesStorageSyncFiles();

        public bool TryRead(string key, out string value)
        {
            value = DarknessRisesStorageRead(key);
            return value != null;
        }

        public bool TryWrite(string key, string value)
        {
            return DarknessRisesStorageWrite(key, value ?? string.Empty) == Stored;
        }

        public void Delete(string key)
        {
            DarknessRisesStorageRemove(key);
        }
    }
}
#endif
