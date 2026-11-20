// The Web player's bridge to the browser's localStorage. It only gets, sets and removes text under a key.
// Every call is guarded: localStorage can be blocked (a private window, storage turned off for the site) or full,
// and then it answers "nothing there" or "not stored" instead of throwing into the game.
mergeInto(LibraryManager.library, {
  // The text under the key, or null (a zero pointer) when there is none or it can't be read.
  DarknessRisesStorageRead: function (keyPointer) {
    try {
      var value = window.localStorage.getItem(UTF8ToString(keyPointer));
      if (value === null) {
        return 0;
      }

      var size = lengthBytesUTF8(value) + 1;
      var buffer = _malloc(size);
      stringToUTF8(value, buffer, size);
      return buffer;
    } catch (error) {
      return 0;
    }
  },

  // 1 when the text was stored under the key, 0 when it wasn't (storage blocked or full).
  DarknessRisesStorageWrite: function (keyPointer, valuePointer) {
    try {
      window.localStorage.setItem(UTF8ToString(keyPointer), UTF8ToString(valuePointer));
      return 1;
    } catch (error) {
      return 0;
    }
  },

  DarknessRisesStorageRemove: function (keyPointer) {
    try {
      window.localStorage.removeItem(UTF8ToString(keyPointer));
    } catch (error) {
      // Nothing to remove from storage that can't be reached.
    }
  },

  // Puts the player's files (Application.persistentDataPath) into IndexedDB. Unity keeps them in memory until the
  // file system is synced, and the page's template doesn't sync on its own.
  DarknessRisesStorageSyncFiles: function () {
    try {
      FS.syncfs(false, function (error) {});
    } catch (error) {
      // No file system to sync, or IndexedDB is blocked: the files stay in memory for this visit.
    }
  }
});
