namespace DarknessRises.Game.Haptics
{
    /// <summary>Why an effect didn't play.</summary>
    public enum HapticDrop
    {
        /// <summary>The VIBRATION switch is off.</summary>
        Off,

        /// <summary>This device can't vibrate.</summary>
        Unsupported,

        /// <summary>Held: paused, an ad, the loading screen or the background.</summary>
        Held,

        /// <summary>Nothing is authored for it, or ticks on buttons are off.</summary>
        Unauthored,

        /// <summary>The same effect played too recently.</summary>
        Cooldown,

        /// <summary>An ambient effect, and something vibrated too recently.</summary>
        NotQuiet,

        /// <summary>A stronger effect is still playing.</summary>
        Weaker,

        /// <summary>Too soon after the last effect.</summary>
        TooSoon,

        /// <summary>Over the cap of effects in a second.</summary>
        OverCap,
    }
}
