namespace DarknessRises.Game.Saving
{
    /// <summary>
    /// A save kept in memory only: for a scene played on its own in the Editor, which has no boot to own the real save,
    /// and for tests. It's gone when whoever holds it is.
    /// </summary>
    public sealed class MemorySaveStorage : ISaveStorage
    {
        /// <summary>The text stored, or null when nothing is.</summary>
        public string Text { get; set; }

        /// <summary>How many times text has been stored.</summary>
        public int WriteCount { get; private set; }

        /// <summary>When set, storing fails and what was stored stays, like a full disk.</summary>
        public bool FailsWrites { get; set; }

        public bool TryRead(out string text)
        {
            text = Text;
            return text != null;
        }

        public bool TryWrite(string text)
        {
            if (FailsWrites)
            {
                return false;
            }

            Text = text;
            WriteCount++;
            return true;
        }

        public void Delete()
        {
            Text = null;
        }
    }
}
