namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// The three groups every sound in the game belongs to, each with its own volume slider. The numbers are stored in
    /// scenes and prefabs (on every <see cref="AudioCategorySource"/>), so they never change.
    /// </summary>
    public enum AudioCategory
    {
        /// <summary>What the player's actions and the threats make: Weapons, Grenades, Enemies, the Boss, banners and menus.</summary>
        Sound = 0,

        /// <summary>The world around: the storm bed and its Rumble, and each Room's own ambience.</summary>
        Ambient = 1,

        /// <summary>Music. There is none yet; the category is ready for it.</summary>
        Music = 2,
    }
}
