namespace DarknessRises.Game.Weapons
{
    /// <summary>Something a Shot can damage, found on the collider the Shot hits.</summary>
    public interface IShotTarget
    {
        /// <returns>Whether the Shot hurt it. A Shot that doesn't hurt shows a dull hit spark.</returns>
        bool TakeShot(int damage);
    }
}
