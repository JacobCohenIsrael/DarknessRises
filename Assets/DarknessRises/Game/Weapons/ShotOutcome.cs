namespace DarknessRises.Game.Weapons
{
    /// <summary>How one Shot ended, for the Score: a hit, a miss, or neither.</summary>
    public enum ShotOutcome
    {
        /// <summary>It hurt nothing: it hit a wall, ran out of range, or struck something it can't hurt.</summary>
        Missed,

        /// <summary>It hurt an Enemy.</summary>
        HurtEnemy,

        /// <summary>
        /// It struck something that counts as neither a hit nor a miss, such as a Critter: the Combo, accuracy and the
        /// Grade don't change.
        /// </summary>
        Neutral,
    }
}
