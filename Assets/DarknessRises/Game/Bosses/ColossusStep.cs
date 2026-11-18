namespace DarknessRises.Game.Bosses
{
    /// <summary>Where the Colossus is in its stomp.</summary>
    public enum ColossusStep
    {
        /// <summary>Not fighting yet.</summary>
        Waiting,

        /// <summary>Between stomps.</summary>
        Resting,

        /// <summary>The growl: the Rumble before the flash.</summary>
        Growling,

        /// <summary>The flash showed the raised foot's shadow; the Relocate window is open.</summary>
        Raised,

        /// <summary>The Relocate window has closed; the foot comes down.</summary>
        Falling,

        /// <summary>Phase 3: the first foot has landed and the second comes down after it.</summary>
        Chaining,

        /// <summary>The foot is down with its weak spot lit: the damage window.</summary>
        Planted,

        /// <summary>The foot lifts back out of the Room.</summary>
        Lifting,

        /// <summary>Beaten.</summary>
        Defeated,
    }
}
