using System;
using System.Collections.Generic;

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
        private const int CreditsVersion = 1;

        /// <summary>Adds what was bought in the Shop.</summary>
        private const int ShopVersion = 2;

        /// <summary>Slow-mo became Adrenaline: its Upgrades' ids changed.</summary>
        private const int AdrenalineVersion = 3;

        /// <summary>Adds the best Score of any Run.</summary>
        private const int BestScoreVersion = 4;

        // The Upgrade ids version 3 renamed, old to new. The levels bought carry over unchanged.
        private static readonly (string from, string to)[] AdrenalineIds =
        {
            ("slow-mo.duration", "adrenaline.duration"),
            ("slow-mo.cooldown", "adrenaline.cooldown"),
        };

        /// <summary>Migrates <paramref name="record"/> in place to <see cref="SaveRecord.CurrentVersion"/> and returns it.</summary>
        public static SaveRecord ToCurrent(SaveRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            // Text without a version predates nothing: the first version's fields are all it could hold.
            if (record.version < CreditsVersion)
            {
                record.version = CreditsVersion;
            }

            if (record.version == CreditsVersion)
            {
                // Before the Shop nothing could be bought.
                record.items = new List<SaveItem>();
                record.version = ShopVersion;
            }

            if (record.version == ShopVersion)
            {
                RenameItems(record, AdrenalineIds);
                record.version = AdrenalineVersion;
            }

            if (record.version == AdrenalineVersion)
            {
                // No Run before this version kept its Score: the best starts from nothing.
                record.bestScore = 0;
                record.version = BestScoreVersion;
            }

            if (record.version > SaveRecord.CurrentVersion)
            {
                record.version = SaveRecord.CurrentVersion;
            }

            PutRight(record);
            return record;
        }

        private static void RenameItems(SaveRecord record, (string from, string to)[] renames)
        {
            if (record.items == null)
            {
                return;
            }

            foreach (var item in record.items)
            {
                if (item == null)
                {
                    continue;
                }

                foreach (var (from, to) in renames)
                {
                    if (item.id == from)
                    {
                        item.id = to;
                    }
                }
            }
        }

        private static void PutRight(SaveRecord record)
        {
            record.credits = Math.Max(0, record.credits);
            record.runsFinished = Math.Max(0, record.runsFinished);
            record.bestScore = Math.Max(0, record.bestScore);
            record.items ??= new List<SaveItem>();

            // One entry per item, at its highest level; entries without an id or a level are dropped.
            var kept = new List<SaveItem>(record.items.Count);
            foreach (var item in record.items)
            {
                if (item == null || string.IsNullOrEmpty(item.id) || item.level <= 0)
                {
                    continue;
                }

                var existing = kept.Find(other => other.id == item.id);
                if (existing == null)
                {
                    kept.Add(item);
                }
                else
                {
                    existing.level = Math.Max(existing.level, item.level);
                }
            }

            record.items = kept;
        }
    }
}
