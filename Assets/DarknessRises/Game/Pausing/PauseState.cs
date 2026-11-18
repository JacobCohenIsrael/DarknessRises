namespace DarknessRises.Game.Pausing
{
    /// <summary>Whether the Level plays, is paused with the pause menu up, or was left from the pause menu.</summary>
    public enum PauseState
    {
        /// <summary>The Level plays.</summary>
        Playing,

        /// <summary>Everything in the Level is frozen and the pause menu is up.</summary>
        Paused,

        /// <summary>
        /// The player picked Restart or Exit on the pause menu. The Level stays frozen behind whatever comes next
        /// (the end screen, or the next Run loading) until its scene goes.
        /// </summary>
        Left,
    }
}
