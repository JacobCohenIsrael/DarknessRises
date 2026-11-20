# The save: one versioned record behind an interface, a JSON file on the device

Credits and everything the Shop sells are the game's first durable player progress, so where and how they're kept has to survive app updates and leave room for cloud save.

The save is one small **versioned record** (`SaveRecord`: a version number and plain fields). The game never works on the record itself. `PlayerSave`, plain C# ([ADR 0003](0003-plain-csharp-rules-and-authored-definitions.md)), reads it once at boot, holds the live state (the Credits in the bank, and later the Upgrades and Weapons the player owns), and writes a fresh record at the moments that matter: a Run banks its Credits, something is bought, the app goes to the background or quits. The boot owns it, because it outlives every Run and Level, and hands it to what needs it. There is no global access to it.

Storage sits behind `ISaveStorage`, which only reads and writes text. Today the adapter is a JSON file, `save.json` under `Application.persistentDataPath`, written atomically: the text goes to a temporary file that then replaces the save in one step, and the save it replaced stays beside it as a backup. A write that fails loses nothing in memory and is retried at the next save point. Tests, and a scene played on its own in the Editor, use an in-memory storage instead.

A record older than the build is migrated one version step at a time in `SaveMigration`; every new version adds a step and a test with a record of the version before it. Text that isn't a save is treated as no save. A record from a newer build is read as far as the older build understands it; downgrading isn't supported beyond that.

Credits are banked from the Run, not counted in the save as they're earned: a Run keeps its own tally (`RunCredits`) and banking takes only what hasn't been banked yet. That makes banking safe to repeat (on the way to the background mid-Run, when the Run ends, after the end-of-Run ad), so no way of ending a Run loses Credits and none is banked twice.

## Considered Options

- **`PlayerPrefs`**: rejected for progress. It has no atomic write of a whole record, no versioning, and on some platforms sits in the registry or a plist that's awkward to back up or move to the cloud. It stays fine for device-only preferences such as where the Grenade button was dropped.
- **A binary format or an encrypted file**: rejected for now. The save is tiny, JSON is readable in a bug report, and an offline single-player game gains little from obscuring it. Tamper-proofing belongs with server-side progress, if that ever comes.
- **Cloud save from the start**: out of scope. The storage interface is where it plugs in later, next to (not instead of) the local file.
- **Saving continuously during a Run**: rejected. The Shop is open only between Runs ([ADR 0005](0005-endless-chain-of-levels.md)), so nothing but the Run's Credits changes mid-Run, and those are banked on the way to the background.
