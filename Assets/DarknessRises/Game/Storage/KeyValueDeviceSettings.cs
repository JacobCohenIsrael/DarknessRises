using System;
using System.Globalization;

namespace DarknessRises.Game.Storage
{
    /// <summary>
    /// The device settings as keyed text: each setting under its own namespaced key (<see cref="BrowserKeys.ForSetting"/>),
    /// its number written the same way whatever the device's language. Text that isn't a number counts as nothing kept.
    /// Plain C# (ADR 0003).
    /// </summary>
    public sealed class KeyValueDeviceSettings : IDeviceSettings
    {
        // Round-trips a float exactly.
        private const string FloatFormat = "R";

        private readonly IKeyValueStorage storage;

        public KeyValueDeviceSettings(IKeyValueStorage storage)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public bool TryGetInt(string name, out int value)
        {
            value = 0;
            return storage.TryRead(BrowserKeys.ForSetting(name), out var text)
                && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        public bool TryGetFloat(string name, out float value)
        {
            value = 0f;
            return storage.TryRead(BrowserKeys.ForSetting(name), out var text)
                && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && !float.IsNaN(value)
                && !float.IsInfinity(value);
        }

        public bool SetInt(string name, int value)
        {
            return storage.TryWrite(BrowserKeys.ForSetting(name), value.ToString(CultureInfo.InvariantCulture));
        }

        public bool SetFloat(string name, float value)
        {
            return storage.TryWrite(BrowserKeys.ForSetting(name), value.ToString(FloatFormat, CultureInfo.InvariantCulture));
        }

        public void Remove(string name) => storage.Delete(BrowserKeys.ForSetting(name));

        // Keyed storage keeps a value the moment it's written.
        public void Flush()
        {
        }
    }
}
