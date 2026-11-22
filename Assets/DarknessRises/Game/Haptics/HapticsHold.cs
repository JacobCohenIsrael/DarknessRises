using System;

namespace DarknessRises.Game.Haptics
{
    /// <summary>Why no effect may play now. Each is held and let go by its own owner.</summary>
    [Flags]
    public enum HapticsHold
    {
        None = 0,

        /// <summary>The Level is frozen: the pause menu, or held for the Score boost's ad.</summary>
        Paused = 1,

        /// <summary>An ad plays (a Continue's).</summary>
        Ad = 2,

        /// <summary>The loading screen is up.</summary>
        Loading = 4,

        /// <summary>The app is in the background.</summary>
        Background = 8,
    }
}
