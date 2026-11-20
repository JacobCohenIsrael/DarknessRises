namespace DarknessRises.Game.Storage
{
    /// <summary>
    /// Text kept under a key, somewhere that outlives the app: the browser's <c>localStorage</c> on the Web player.
    /// It only stores and returns text. It never throws: storage that is blocked or full answers false.
    /// </summary>
    public interface IKeyValueStorage
    {
        /// <summary>Reads the text under <paramref name="key"/>. False when nothing is there or it can't be read.</summary>
        bool TryRead(string key, out string value);

        /// <summary>
        /// Stores <paramref name="value"/> under <paramref name="key"/> in place of what was there.
        /// False when it couldn't be stored; what was there before is then still there.
        /// </summary>
        bool TryWrite(string key, string value);

        /// <summary>Removes what is under <paramref name="key"/>, if anything.</summary>
        void Delete(string key);
    }
}
