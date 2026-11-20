using System;
using UnityEngine;

namespace DarknessRises.Game.Saving
{
    /// <summary>The save's text form: a <see cref="SaveRecord"/> as JSON.</summary>
    public static class SaveFormat
    {
        /// <summary>The record as text, stamped with the version this build writes.</summary>
        public static string ToText(SaveRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            record.version = SaveRecord.CurrentVersion;
            return JsonUtility.ToJson(record);
        }

        /// <summary>
        /// Reads a record from <paramref name="text"/>, as it was written (not migrated). False when the text isn't a
        /// record at all: empty, cut short or not JSON.
        /// </summary>
        public static bool TryParse(string text, out SaveRecord record)
        {
            record = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            try
            {
                record = JsonUtility.FromJson<SaveRecord>(text);
            }
            catch (ArgumentException)
            {
                // Not JSON.
                return false;
            }

            return record != null;
        }
    }
}
