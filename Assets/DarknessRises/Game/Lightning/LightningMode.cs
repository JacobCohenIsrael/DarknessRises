namespace DarknessRises.Game.Lightning
{
    /// <summary>Who decides when Lightning flashes.</summary>
    public enum LightningMode
    {
        /// <summary>The weather: random intervals, only in Rooms that let Lightning in.</summary>
        Ambient,

        /// <summary>The Boss's pattern: Lightning flashes only when the Boss requests it.</summary>
        BossDriven,
    }
}
