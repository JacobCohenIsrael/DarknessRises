namespace DarknessRises.Game.Weapons
{
    /// <summary>Something a Shot or a Grenade's blast can damage, found on the collider it hits.</summary>
    public interface IShotTarget
    {
        /// <returns>Whether the hit hurt it. A Shot that doesn't hurt shows a dull hit spark.</returns>
        bool TakeShot(in ShotHit hit);
    }
}
