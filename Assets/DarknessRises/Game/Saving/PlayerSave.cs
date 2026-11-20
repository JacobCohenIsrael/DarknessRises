using System;
using System.Collections.Generic;
using DarknessRises.Game.Runs;

namespace DarknessRises.Game.Saving
{
    /// <summary>
    /// The player's durable progress, kept across app restarts: the Credits in the bank, how many Runs they've
    /// finished, the level of everything bought in the Shop, and the Primary weapon chosen for the next Run. It's read
    /// from its <see cref="ISaveStorage"/> once, and written back whenever it changes at a moment that matters: a Run
    /// banks its Credits, something is bought or chosen, the app goes to the background or quits.
    /// Plain C# (ADR 0003): whoever lives longest (the boot) owns it and hands it to what needs it.
    /// </summary>
    /// <remarks>
    /// It holds what the player has, not what anything costs or does: prices and effects are authored, and the Shop's
    /// rules decide what may be bought. A write that fails (a full disk) loses nothing in memory: the save stays marked
    /// as changed and is written again at the next save point.
    /// </remarks>
    public sealed class PlayerSave
    {
        private readonly ISaveStorage storage;

        // The level of each thing bought in the Shop, by its authored id, in the order they were first bought.
        private readonly Dictionary<string, int> levels = new();
        private readonly List<string> boughtIds = new();
        private bool hasUnsavedChanges;

        private PlayerSave(ISaveStorage saveStorage, SaveRecord record)
        {
            storage = saveStorage;
            Credits = record.credits;
            RunsFinished = record.runsFinished;
            PrimaryWeaponId = record.primaryWeapon ?? string.Empty;
            foreach (var item in record.items)
            {
                levels[item.id] = item.level;
                boughtIds.Add(item.id);
            }
        }

        /// <summary>Raised when the Credits in the bank change, with the new total.</summary>
        public event Action<int> CreditsChanged;

        /// <summary>Raised when anything the save holds changes: the bank, what was bought, the chosen Primary, the Runs finished.</summary>
        public event Action Changed;

        /// <summary>Credits in the bank: every Run's earnings so far, less what was spent in the Shop.</summary>
        public int Credits { get; private set; }

        /// <summary>How many Runs the player has finished, whichever way they ended.</summary>
        public int RunsFinished { get; private set; }

        /// <summary>The id of the Primary weapon chosen for the next Run. Empty until one is chosen.</summary>
        public string PrimaryWeaponId { get; private set; }

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
            MarkChanged();
            Bank(runCredits);
        }

        /// <summary>The level the item with <paramref name="itemId"/> was bought up to. 0 when it was never bought.</summary>
        public int LevelOf(string itemId)
        {
            return itemId != null && levels.TryGetValue(itemId, out var level) ? level : 0;
        }

        /// <summary>
        /// Pays <paramref name="price"/> from the bank and raises the item to <paramref name="level"/>, both or
        /// neither. Whether the item may be bought at all is the Shop's call; this only refuses a price the bank can't
        /// cover. It doesn't write the save: the Shop does, once the purchase is done.
        /// </summary>
        public bool Purchase(string itemId, int level, int price)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentException("A Shop item needs an id to be saved under.", nameof(itemId));
            }

            if (level <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, "A bought item is at least at level 1.");
            }

            if (price < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(price), price, "Nothing in the Shop pays the player.");
            }

            if (Credits < price)
            {
                return false;
            }

            if (!levels.ContainsKey(itemId))
            {
                boughtIds.Add(itemId);
            }

            levels[itemId] = level;
            if (price > 0)
            {
                Credits -= price;
                CreditsChanged?.Invoke(Credits);
            }

            MarkChanged();
            return true;
        }

        /// <summary>
        /// Puts <paramref name="credits"/> straight in the bank, outside any Run. For the dev tools; a Run's Credits come
        /// in through <see cref="Bank"/>. It doesn't write the save.
        /// </summary>
        public void AddCredits(int credits)
        {
            if (credits < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(credits), credits, "Credits are spent in the Shop, not taken away.");
            }

            Deposit(credits);
        }

        /// <summary>
        /// Puts the save back to a first install: no Credits, nothing bought, no Primary chosen, no Run finished. For the
        /// dev tools. It doesn't write the save.
        /// </summary>
        public void Reset()
        {
            var hadCredits = Credits != 0;
            Credits = 0;
            RunsFinished = 0;
            PrimaryWeaponId = string.Empty;
            levels.Clear();
            boughtIds.Clear();
            if (hadCredits)
            {
                CreditsChanged?.Invoke(Credits);
            }

            MarkChanged();
        }

        /// <summary>Chooses the Primary weapon for the next Run, by its id. Whether it's owned is the Shop's call.</summary>
        /// <returns>Whether the choice changed.</returns>
        public bool ChoosePrimaryWeapon(string weaponId)
        {
            weaponId ??= string.Empty;
            if (weaponId == PrimaryWeaponId)
            {
                return false;
            }

            PrimaryWeaponId = weaponId;
            MarkChanged();
            return true;
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
            CreditsChanged?.Invoke(Credits);
            MarkChanged();
        }

        private void MarkChanged()
        {
            hasUnsavedChanges = true;
            Changed?.Invoke();
        }

        private SaveRecord ToRecord()
        {
            var record = new SaveRecord
            {
                version = SaveRecord.CurrentVersion,
                credits = Credits,
                runsFinished = RunsFinished,
                primaryWeapon = PrimaryWeaponId,
            };

            foreach (var id in boughtIds)
            {
                record.items.Add(new SaveItem { id = id, level = levels[id] });
            }

            return record;
        }
    }
}
