using System;

namespace DarknessRises.Game.Saving
{
    /// <summary>
    /// A save storage whose writes only reach the device once something else puts them there: the Web player's files,
    /// which live in memory until the file system is synced to the browser. It asks for that sync after every write
    /// and delete that worked. Plain C# (ADR 0003).
    /// </summary>
    public sealed class SyncedSaveStorage : ISaveStorage
    {
        private readonly ISaveStorage inner;
        private readonly Action sync;

        /// <param name="inner">Where the save is kept.</param>
        /// <param name="sync">Puts what was written on the device. Called after each write and delete that worked.</param>
        public SyncedSaveStorage(ISaveStorage inner, Action sync)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
            this.sync = sync ?? throw new ArgumentNullException(nameof(sync));
        }

        public bool TryRead(out string text) => inner.TryRead(out text);

        public bool TryWrite(string text)
        {
            if (!inner.TryWrite(text))
            {
                return false;
            }

            sync();
            return true;
        }

        public void Delete()
        {
            inner.Delete();
            sync();
        }
    }
}
