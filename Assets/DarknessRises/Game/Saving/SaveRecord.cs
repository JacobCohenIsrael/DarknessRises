using System;

namespace DarknessRises.Game.Saving
{
    /// <summary>
    /// What the save holds on the device, as it's written: plain fields and a version number, so a later build can tell
    /// an older record apart and migrate it (see <see cref="SaveMigration"/>). The game never works on this directly:
    /// <see cref="PlayerSave"/> reads it once and writes a fresh one each time it saves.
    /// </summary>
    /// <remarks>
    /// Public fields because that's what Unity's JSON serializer reads and writes. Adding a field is safe (an older
    /// record reads it as its default); renaming or re-meaning one needs a new version and a migration step.
    /// </remarks>
    [Serializable]
    public sealed class SaveRecord
    {
        /// <summary>The version this build writes.</summary>
        public const int CurrentVersion = 1;

        /// <summary>The version the record was written as. 0 means the text carried none.</summary>
        public int version;

        /// <summary>Credits in the bank.</summary>
        public int credits;

        /// <summary>How many Runs the player has finished, whichever way they ended.</summary>
        public int runsFinished;
    }
}
