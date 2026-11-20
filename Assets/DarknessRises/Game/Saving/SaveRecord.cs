using System;
using System.Collections.Generic;

namespace DarknessRises.Game.Saving
{
    /// <summary>
    /// What the save holds on the device, as it's written: plain fields and a version number, so a later build can tell
    /// an older record apart and migrate it (see <see cref="SaveMigration"/>). The game never works on this directly:
    /// <see cref="PlayerSave"/> reads it once and writes a fresh one each time it saves.
    /// </summary>
    /// <remarks>
    /// Public fields because that's what Unity's JSON serializer reads and writes. Adding a field is safe (an older
    /// record reads it as its default), and so is dropping one (text that still has it reads fine without it);
    /// renaming or re-meaning one needs a new version and a migration step.
    /// </remarks>
    [Serializable]
    public sealed class SaveRecord
    {
        /// <summary>The version this build writes.</summary>
        public const int CurrentVersion = 2;

        /// <summary>The version the record was written as. 0 means the text carried none.</summary>
        public int version;

        /// <summary>Credits in the bank.</summary>
        public int credits;

        /// <summary>How many Runs the player has finished, whichever way they ended.</summary>
        public int runsFinished;

        /// <summary>What was bought in the Shop, each with the level it stands at. Since version 2.</summary>
        public List<SaveItem> items = new();
    }

    /// <summary>One thing bought in the Shop: the item's id, as authored in the Shop definition, and its level.</summary>
    [Serializable]
    public sealed class SaveItem
    {
        public string id;
        public int level;
    }
}
