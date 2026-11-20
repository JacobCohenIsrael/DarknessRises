namespace DarknessRises.Game.Saving
{
    /// <summary>
    /// Where the save's text is kept: a file on the device today, and something else (cloud save) later without the
    /// rules noticing. It only stores and returns text; what the text means is <see cref="PlayerSave"/>'s business.
    /// </summary>
    public interface ISaveStorage
    {
        /// <summary>Reads the stored text. False when nothing is stored or it can't be read.</summary>
        bool TryRead(out string text);

        /// <summary>
        /// Stores <paramref name="text"/> in place of what was there, all of it or none of it.
        /// False when it couldn't be stored; what was there before is then still there.
        /// </summary>
        bool TryWrite(string text);

        /// <summary>Removes what is stored, if anything.</summary>
        void Delete();
    }
}
