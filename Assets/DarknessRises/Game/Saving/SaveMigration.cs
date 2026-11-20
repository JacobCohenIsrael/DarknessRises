using System;

namespace DarknessRises.Game.Saving
{
    /// <summary>
    /// Brings a <see cref="SaveRecord"/> of any version up to the one this build works with, one version step at a
    /// time, and puts right any value no version allows. Plain C# (ADR 0003).
    /// </summary>
    /// <remarks>
    /// A new save version adds one step here (and a test with a record of the version before it). A record written by
    /// a newer build than this one is read as far as this build understands it; going back to an older build isn't
    /// supported beyond that.
    /// </remarks>
    public static class SaveMigration
    {
        /// <summary>The first version there was: Credits and the Runs finished.</summary>
        private const int FirstVersion = 1;

        /// <summary>Migrates <paramref name="record"/> in place to <see cref="SaveRecord.CurrentVersion"/> and returns it.</summary>
        public static SaveRecord ToCurrent(SaveRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            // Text without a version predates nothing: the first version's fields are all it could hold.
            if (record.version < FirstVersion)
            {
                record.version = FirstVersion;
            }

            // Steps from one version to the next go here, in order:
            // if (record.version == 1) { ...; record.version = 2; }

            if (record.version > SaveRecord.CurrentVersion)
            {
                record.version = SaveRecord.CurrentVersion;
            }

            record.credits = Math.Max(0, record.credits);
            record.runsFinished = Math.Max(0, record.runsFinished);
            return record;
        }
    }
}
