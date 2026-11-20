# The save: one versioned record behind an interface, a JSON file on the device

Credits and everything the Shop sells are the game's first durable player progress, so where and how they're kept has to survive app updates and leave room for cloud save.

The save is one small **versioned record** (`SaveRecord`: a version number and plain fields). The game never works on the record itself. `PlayerSave`, plain C# ([ADR 0003](0003-plain-csharp-rules-and-authored-definitions.md)), reads it once at boot, holds the live state (the Credits in the bank, and later the Upgrades and Weapons the player owns), and writes a fresh record at the moments that matter: a Run banks its Credits, something is bought, the app goes to the background or quits. The boot owns it, because it outlives every Run and Level, and hands it to what needs it. There is no global access to it.

Storage sits behind `ISaveStorage`, which only reads and writes text. Today the adapter is a JSON file, `save.json` under `Application.persistentDataPath` (on the Web player it's the browser's `localStorage` instead: see the amendment below), written atomically: the text goes to a temporary file that then replaces the save in one step, and the save it replaced stays beside it as a backup. A write that fails loses nothing in memory and is retried at the next save point. Tests, and a scene played on its own in the Editor, use an in-memory storage instead.

A record older than the build is migrated one version step at a time in `SaveMigration`; every new version adds a step and a test with a record of the version before it. Text that isn't a save is treated as no save. A record from a newer build is read as far as the older build understands it; downgrading isn't supported beyond that.

Credits are banked from the Run, not counted in the save as they're earned: a Run keeps its own tally (`RunCredits`) and banking takes only what hasn't been banked yet. That makes banking safe to repeat (on the way to the background mid-Run, when the Run ends, after the end-of-Run ad), so no way of ending a Run loses Credits and none is banked twice.

## Considered Options

- **`PlayerPrefs`**: rejected for progress. It has no atomic write of a whole record, no versioning, and on some platforms sits in the registry or a plist that's awkward to back up or move to the cloud. It stays fine for device-only preferences such as where the Grenade button was dropped (the device settings of the amendment below).
- **A binary format or an encrypted file**: rejected for now. The save is tiny, JSON is readable in a bug report, and an offline single-player game gains little from obscuring it. Tamper-proofing belongs with server-side progress, if that ever comes.
- **Cloud save from the start**: out of scope. The storage interface is where it plugs in later, next to (not instead of) the local file.
- **Saving continuously during a Run**: rejected. The Shop is open only between Runs ([ADR 0005](0005-endless-chain-of-levels.md)), so nothing but the Run's Credits changes mid-Run, and those are banked on the way to the background.

## Amendment (2026-11-20): on the Web, the browser's `localStorage`

On the Web player the JSON file didn't work as a save. Two things were wrong, both measured with build 0.1.14 in a headless Chrome (issue #110):

- **A new upload starts empty.** `Application.persistentDataPath` on the Web is a folder named with a hash of the address of the page's folder, in a file system Unity keeps in IndexedDB. `PlayerPrefs` is a file in the same place. itch.io serves every upload from a new address, so every new version looked in a new, empty folder.
- **The save reached the browser only by accident.** Files written there stay in memory until the file system is synced to IndexedDB. Unity's page template leaves `autoSyncPersistentDataPath` off, and the game never asked for a sync. The only one it caused was a side effect of `PlayerPrefs.Save()`: moving a volume slider put the save in the browser along with the setting. Without that, a Run ended, the save was written, IndexedDB stayed empty, and reloading the same page started from nothing.

So on the Web player the save is kept in the browser's **`localStorage`**, under the fixed key `DarknessRises.save.v1`:

- `localStorage` belongs to the page's origin, not to its address, so every upload of the game finds what the last one left.
- A write is done when the call returns. There is nothing to sync, and a save written as the tab closes still lands.
- The key carries the game's name because every itch.io game is served from the same origin and shares one `localStorage`. The `v1` is the key's own version; the record under it is the same versioned `SaveRecord`, migrated as before.
- The text, `PlayerSave` and the save points don't change. Only the adapter does: `KeyedSaveStorage` keeps the save under a key of an `IKeyValueStorage`, and `BrowserLocalStorage` is that storage on the Web, a thin bridge to a `.jslib` that never throws.

`localStorage` can be blocked (a private window, site data turned off) or full (about 5 MB for the origin, shared with every other game on it). So the save sits behind `FallbackSaveStorage`, plain C# with tests, with the file as its second place:

- The save is read from the browser's storage. If nothing is there but the file has a save, that save is read and carried over at once.
- The save is written to the browser's storage. If that fails it's written to the file, and the browser's older copy is removed so the newer save is the one read next time. If both fail, the save stays in memory and is retried at the next save point, as before.
- A save that does land in the file now asks for the file system to be synced, through the same `.jslib`, so the fallback is worth having.

**Device settings** move the same way. `IDeviceSettings` is the one place for what belongs to the device and not to the player's progress: the volume sliders ([ADR 0010](0010-audio-categories-in-code.md)), where the Grenade button was dropped, whether the Shoot hint was shown. On a phone, a desktop and in the Editor it's `PlayerPrefs`, under the names the settings always had. On the Web player it's `localStorage`, each setting under `DarknessRises.settings.<name>`, with `PlayerPrefs` as the fallback and the same rules as the save.

Every other platform is untouched: `SaveStorages.Choose` and `DeviceSettings.Choose` return the file and `PlayerPrefs` themselves when there is no browser storage. The platform check lives in one place, `BrowserStorage`.

### What it doesn't do

- **Saves from earlier uploads can't be brought back.** Their folder is named after an old address, and many of them never reached the browser at all. A save in the current page's own folder is carried over.
- **It's one browser on one device.** Another browser, a private window or clearing the site's data starts from nothing. Moving a save between devices is still cloud save's job.
- **Browsers keep storage apart for embedded pages.** The game inside its itch.io page and the same build opened in a tab of its own may not share `localStorage`.
- **The save isn't private or tamper-proof.** Any page on the origin can read or change it. That was already accepted above for an offline single-player game.

### Considered Options

- **Turn on `autoSyncPersistentDataPath` with a page template of our own and keep the file**: rejected. It fixes the reload but not the new upload, and adds a template to maintain.
- **A database of our own in IndexedDB**: rejected. It also belongs to the origin, but it's asynchronous: every read and write needs a callback into the game, and a write on the way out of the tab may not land. The save is a few hundred bytes; `localStorage` is enough.
- **Keep the device settings in the save**: rejected again. They aren't progress, and a cloud save shouldn't carry one device's volume to another.
