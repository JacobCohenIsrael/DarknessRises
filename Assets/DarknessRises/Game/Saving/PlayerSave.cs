using System;
using DarknessRises.Game.Runs;

namespace DarknessRises.Game.Saving
{
    /// <summary>
    /// The player's durable progress, kept across app restarts: the Credits in the bank and how many Runs they've
    /// finished. It's read from its <see cref="ISaveStorage"/> once, and written back whenever it changes at a moment
    /// that matters: a Run banks its Credits, something is bought, the app goes to the background or quits.
    /// Plain C# (ADR 0003): whoever lives longest (the boot) owns it and hands it to what needs it.
    /// </summary>
    /// <remarks>
    /// A write that fails (a full disk) loses nothing in memory: the save stays marked as changed and is written again
    /// at the next save point.
    /// </remarks>
    public sealed class PlayerSave
    {
        private readonly ISaveStorage storage;
        private bool hasUnsavedChanges;

        private PlayerSave(ISaveStorage saveStorage, SaveRecord record)
        {
            storage = saveStorage;
            Credits = record.credits;
            RunsFinished = record.runsFinished;
        }

        /// <summary>Raised when the Credits in the bank change, with the new total.</summary>
        public event Action<int> CreditsChanged;

        /// <summary>Credits in the bank: every Run's earnings so far, less what was spent in the Shop.</summary>
        public int Credits { get; private set; }

        /// <summary>How many Runs the player has finished, whichever way they ended.</summary>
        public int RunsFinished { get; private set; }

        /// <summary>
        /// Reads the save from <paramref name="saveStorage"/>, migrating an older record to this build's version.
        /// With nothing stored, or text that isn't a save, the player starts from nothing.
        /// </summary>
        public static PlayerSave Load(ISaveStorage saveStorage)
        {
            if (saveStorage == null)
            {
                throw new ArgumentNullException(nameof(saveStorage));
            }

            var record = saveStorage.TryRead(out var text) && SaveFormat.TryParse(text, out var stored)
                ? SaveMigration.ToCurrent(stored)
                : new SaveRecord { version = SaveRecord.CurrentVersion };
            return new PlayerSave(saveStorage, record);
        }

        /// <summary>
        /// Moves the Credits of a Run that haven't been banked yet into the bank, and saves. Safe to call whenever:
        /// Credits already banked aren't banked again.
        /// </summary>
        /// <returns>The Credits banked by this call.</returns>
        public int Bank(RunCredits runCredits)
        {
            if (runCredits == null)
            {
                throw new ArgumentNullException(nameof(runCredits));
            }

            var banked = runCredits.TakeUnbanked();
            Deposit(banked);
            Write();
            return banked;
        }

        /// <summary>A Run ended, whichever way: it counts as finished, its Credits are banked, and the save is written.</summary>
        public void FinishRun(RunCredits runCredits)
        {
            if (runCredits == null)
            {
                throw new ArgumentNullException(nameof(runCredits));
            }

            RunsFinished = RunsFinished < int.MaxValue ? RunsFinished + 1 : int.MaxValue;
            hasUnsavedChanges = true;
            Bank(runCredits);
        }

        /// <summary>Writes the save if it changed since it was last written.</summary>
        /// <returns>Whether the stored save is up to date.</returns>
        public bool Write()
        {
            if (!hasUnsavedChanges)
            {
                return true;
            }

            if (!storage.TryWrite(SaveFormat.ToText(ToRecord())))
            {
                return false;
            }

            hasUnsavedChanges = false;
            return true;
        }

        private void Deposit(int credits)
        {
            if (credits <= 0)
            {
                return;
            }

            // The bank never overflows into debt: it stops at the most an int holds.
            Credits = (int)Math.Min(int.MaxValue, (long)Credits + credits);
            hasUnsavedChanges = true;
            CreditsChanged?.Invoke(Credits);
        }

        private SaveRecord ToRecord()
        {
            return new SaveRecord
            {
                version = SaveRecord.CurrentVersion,
                credits = Credits,
                runsFinished = RunsFinished,
            };
        }
    }
}
