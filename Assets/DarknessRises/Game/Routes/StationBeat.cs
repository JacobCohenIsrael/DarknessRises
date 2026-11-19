namespace DarknessRises.Game.Routes
{
    /// <summary>
    /// The beats of the Station rhythm, in the order every Station with Waves plays them: the character walks, stops and
    /// gets ready, fights, hears the clear, takes a quiet moment, gets the go, and walks again.
    /// </summary>
    public enum StationBeat
    {
        /// <summary>The character walks the Route. No Station's beats are playing.</summary>
        Walking,

        /// <summary>The character has just stopped at a Station and planted: the warning before its first Wave.</summary>
        Ready,

        /// <summary>The Station's Waves are coming. It lasts until the last one is dead.</summary>
        Fight,

        /// <summary>The Station's last Wave is dead: the clear is called.</summary>
        Clear,

        /// <summary>A short moment of quiet after the clear.</summary>
        Quiet,

        /// <summary>The cue to move on, pointing along the Route. When it ends the Station is cleared and the character sets off.</summary>
        Go,
    }
}
