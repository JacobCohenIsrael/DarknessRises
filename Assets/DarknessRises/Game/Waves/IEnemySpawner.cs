using DarknessRises.Game.Enemies;

namespace DarknessRises.Game.Waves
{
    /// <summary>Brings Enemies into the Room for a <see cref="WaveSchedule{TEnemyType}"/>.</summary>
    public interface IEnemySpawner<in TEnemyType>
    {
        /// <summary>
        /// Brings one Enemy of the type in through a Spawn point, alive.
        /// Returns null when it can't right now, for example because every Spawn point is lit; the schedule tries again.
        /// </summary>
        Enemy TrySpawn(TEnemyType enemyType);
    }
}
