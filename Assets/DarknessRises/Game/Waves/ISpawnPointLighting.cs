namespace DarknessRises.Game.Waves
{
    /// <summary>Tells whether a Room's Spawn point is lit at this moment.</summary>
    public interface ISpawnPointLighting
    {
        /// <param name="spawnPoint">Index of the Spawn point in its Room.</param>
        bool IsLit(int spawnPoint);
    }
}
