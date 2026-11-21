namespace DarknessRises.Game.Content
{
    /// <summary>
    /// The Addressables labels that name what a destination can need, so the boot can measure and download it all
    /// before it loads that destination's scene (ADR 0007). The editor setup puts them on content by its group.
    /// </summary>
    public static class ContentLabels
    {
        /// <summary>Everything any Level can load: the Level scenes and their assets, every Enemy type, and the Shared content.</summary>
        public const string Level = "Level";

        /// <summary>Everything the Shop can load: its scene and the Shared content.</summary>
        public const string Shop = "Shop";
    }
}
