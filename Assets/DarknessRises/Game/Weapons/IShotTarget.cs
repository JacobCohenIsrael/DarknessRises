namespace DarknessRises.Game.Weapons
{
    /// <summary>Something a Shot can damage, found on the collider the Shot hits.</summary>
    public interface IShotTarget
    {
        void TakeShot(int damage);
    }
}
