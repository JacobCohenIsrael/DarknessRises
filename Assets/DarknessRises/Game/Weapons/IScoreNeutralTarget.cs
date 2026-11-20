namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// Something a Shot can strike that counts as neither a hit nor a miss (<see cref="ShotOutcome.Neutral"/>), such as a
    /// Critter: it isn't an Enemy, so shooting it costs and earns nothing. Found next to its <see cref="IShotTarget"/>.
    /// </summary>
    public interface IScoreNeutralTarget
    {
    }
}
