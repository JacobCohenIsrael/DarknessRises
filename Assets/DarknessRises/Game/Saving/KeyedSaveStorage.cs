using System;
using DarknessRises.Game.Storage;

namespace DarknessRises.Game.Saving
{
    /// <summary>
    /// The save as text under one key of a keyed storage: the browser's <c>localStorage</c> on the Web player.
    /// A thin adapter: it only reads and writes text. Setting a key replaces its whole value in one step, so the save
    /// under it is always a whole one.
    /// </summary>
    public sealed class KeyedSaveStorage : ISaveStorage
    {
        private readonly IKeyValueStorage storage;
        private readonly string key;

        /// <param name="storage">Where the text is kept.</param>
        /// <param name="key">The key the save is kept under.</param>
        public KeyedSaveStorage(IKeyValueStorage storage, string key)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("The save needs a key.", nameof(key));
            }

            this.key = key;
        }

        public bool TryRead(out string text) => storage.TryRead(key, out text);

        public bool TryWrite(string text) => storage.TryWrite(key, text);

        public void Delete() => storage.Delete(key);
    }
}
