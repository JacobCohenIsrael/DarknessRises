using System.Collections.Generic;

namespace DarknessRises.Game.Storage
{
    /// <summary>Device settings kept in memory only: for tests, where they stand in for <c>PlayerPrefs</c>.</summary>
    public sealed class MemoryDeviceSettings : IDeviceSettings
    {
        private readonly Dictionary<string, float> values = new();

        /// <summary>When set, nothing can be set and what is kept stays, like storage that is full.</summary>
        public bool FailsWrites { get; set; }

        /// <summary>How many settings are kept.</summary>
        public int Count => values.Count;

        /// <summary>How many times <see cref="Flush"/> was called.</summary>
        public int Flushes { get; private set; }

        public bool Has(string name) => values.ContainsKey(name);

        public bool TryGetInt(string name, out int value)
        {
            var found = values.TryGetValue(name, out var kept);
            value = found ? (int)kept : 0;
            return found;
        }

        public bool TryGetFloat(string name, out float value) => values.TryGetValue(name, out value);

        public bool SetInt(string name, int value) => SetFloat(name, value);

        public bool SetFloat(string name, float value)
        {
            if (FailsWrites)
            {
                return false;
            }

            values[name] = value;
            return true;
        }

        public void Remove(string name) => values.Remove(name);

        public void Flush() => Flushes++;
    }
}
