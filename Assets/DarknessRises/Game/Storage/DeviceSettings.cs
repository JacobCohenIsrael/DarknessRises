namespace DarknessRises.Game.Storage
{
    /// <summary>Picks where the device settings are kept on the platform the game runs on.</summary>
    public static class DeviceSettings
    {
        /// <summary>
        /// The device settings of this player: <c>PlayerPrefs</c> on a phone, a desktop and in the Editor; the browser's
        /// storage, with <c>PlayerPrefs</c> to fall back on, on the Web player. What it returns holds no state of its
        /// own, so whoever needs the settings can ask for them.
        /// </summary>
        public static IDeviceSettings OfThisPlayer()
        {
            return Choose(BrowserStorage.OfThisPlayer(), new PlayerPrefsDeviceSettings());
        }

        /// <summary>
        /// With no browser storage (every platform but the Web) the settings are the device's own, untouched. With it,
        /// they're kept in the browser's storage first and fall back to the device's own.
        /// </summary>
        public static IDeviceSettings Choose(IKeyValueStorage browser, IDeviceSettings ownSettings)
        {
            return browser == null ? ownSettings : new FallbackDeviceSettings(new KeyValueDeviceSettings(browser), ownSettings);
        }
    }
}
