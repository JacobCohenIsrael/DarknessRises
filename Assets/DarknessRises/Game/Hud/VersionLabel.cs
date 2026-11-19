namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// The rule for the build version label's text: <c>v</c>, the build's version, and <c>-dev</c> in a development build,
    /// so a player can tell which build is loaded (a browser may keep serving an older one from its cache).
    /// </summary>
    public static class VersionLabel
    {
        private const string Prefix = "v";
        private const string DevelopmentSuffix = "-dev";

        /// <summary>The label for <paramref name="version"/>, such as <c>v0.1.8</c>, or <c>v0.1.8-dev</c> in a development build.</summary>
        public static string Text(string version, bool isDevelopmentBuild)
        {
            return isDevelopmentBuild ? Prefix + version + DevelopmentSuffix : Prefix + version;
        }
    }
}
