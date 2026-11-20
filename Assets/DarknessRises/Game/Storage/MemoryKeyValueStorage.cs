using System.Collections.Generic;

namespace DarknessRises.Game.Storage
{
    /// <summary>
    /// Keyed text kept in memory only: for tests, where it stands in for the browser's storage. It can be told to
    /// behave like storage that is full (it keeps what it has and takes nothing new) or blocked (nothing in, nothing out).
    /// </summary>
    public sealed class MemoryKeyValueStorage : IKeyValueStorage
    {
        private readonly Dictionary<string, string> values = new();

        /// <summary>When set, storing fails and what was stored stays: storage that is full.</summary>
        public bool FailsWrites { get; set; }

        /// <summary>When set, nothing can be read, stored or removed: storage the browser blocks, as in a private window.</summary>
        public bool IsBlocked { get; set; }

        /// <summary>How many values are stored.</summary>
        public int Count => values.Count;

        /// <summary>How many times a value has been stored.</summary>
        public int WriteCount { get; private set; }

        /// <summary>The text under <paramref name="key"/>, or null when nothing is, whatever the storage is pretending.</summary>
        public string this[string key]
        {
            get => values.TryGetValue(key, out var value) ? value : null;
            set => values[key] = value;
        }

        public bool TryRead(string key, out string value)
        {
            if (IsBlocked)
            {
                value = null;
                return false;
            }

            return values.TryGetValue(key, out value);
        }

        public bool TryWrite(string key, string value)
        {
            if (IsBlocked || FailsWrites)
            {
                return false;
            }

            values[key] = value;
            WriteCount++;
            return true;
        }

        public void Delete(string key)
        {
            if (!IsBlocked)
            {
                values.Remove(key);
            }
        }
    }
}
