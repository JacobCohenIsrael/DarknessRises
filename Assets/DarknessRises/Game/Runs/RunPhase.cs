namespace DarknessRises.Game.Runs
{
    /// <summary>Where a Run is: which part of the current Level is playing, or whether the character is dead or the Run over.</summary>
    public enum RunPhase
    {
        /// <summary>No Run has started yet.</summary>
        NotStarted,

        /// <summary>The Level's start beat: its title card, as the character walks in through the door.</summary>
        LevelStart,

        /// <summary>The character walks the Route between Stations, with Stragglers coming at it.</summary>
        Walking,

        /// <summary>The character stands at a Station and fights its Waves.</summary>
        StationFight,

        /// <summary>The Level's Boss fight.</summary>
        BossFight,

        /// <summary>The Level's end beat: the calm after the last fight, then the walk out the far door.</summary>
        LevelEnd,

        /// <summary>The character has died. The player picks Continue (once per Run), Restart or Exit.</summary>
        Dead,

        /// <summary>The Run is over after Exit: the end screen shows the Level reached and the Enemies killed.</summary>
        Ended,
    }
}
