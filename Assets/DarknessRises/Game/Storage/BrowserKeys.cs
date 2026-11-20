using System;

namespace DarknessRises.Game.Storage
{
    /// <summary>
    /// The keys the Web player uses in the browser's <c>localStorage</c>. Every one starts with the game's namespace:
    /// on itch.io every game is served from the same origin and shares one <c>localStorage</c>, so a bare key such as
    /// "save" could be another game's. The keys are fixed, so every upload of the game finds what the last one left.
    /// </summary>
    public static class BrowserKeys
    {
        /// <summary>What every key of the game starts with.</summary>
        public const string Namespace = "DarknessRises.";

        /// <summary>
        /// The key of the save. The number is the key's own version, for the day the save moves somewhere else; the
        /// record under it carries its own version and migrates as it always has (ADR 0009).
        /// </summary>
        public const string Save = Namespace + "save.v1";

        /// <summary>What the key of every device setting starts with.</summary>
        public const string SettingsPrefix = Namespace + "settings.";

        /// <summary>
        /// The key of the device setting named <paramref name="name"/>. A name that already starts with the game's
        /// namespace isn't given it twice.
        /// </summary>
        public static string ForSetting(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("A device setting needs a name.", nameof(name));
            }

            var bare = name.StartsWith(Namespace, StringComparison.Ordinal) ? name.Substring(Namespace.Length) : name;
            return SettingsPrefix + bare;
        }
    }
}
