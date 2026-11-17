using DarknessRises.Game.Waves;
using UnityEngine;

namespace DarknessRises.Game.Levels
{
    /// <summary>
    /// The Spawn points a Straggler may enter through: dark ones ahead of the character along the way it walks,
    /// so the Straggler comes at it instead of trailing behind. To the <see cref="SpawnPointPicker"/>,
    /// every other Spawn point counts as closed. Checking never allocates.
    /// </summary>
    public sealed class StragglerEntrances : ISpawnPointLighting
    {
        private readonly ISpawnPointLighting lighting;
        private readonly RoomSpawnPoints spawnPoints;
        private readonly float minMetresAhead;
        private Vector3 characterPosition;
        private Vector3 walkingDirection;

        public StragglerEntrances(ISpawnPointLighting lighting, RoomSpawnPoints spawnPoints, float minMetresAhead)
        {
            this.lighting = lighting;
            this.spawnPoints = spawnPoints;
            this.minMetresAhead = minMetresAhead;
        }

        /// <summary>Where the character is and which way it walks, for the next pick.</summary>
        public void Follow(Vector3 position, Vector3 direction)
        {
            characterPosition = position;
            walkingDirection = direction;
        }

        public bool IsLit(int spawnPoint)
        {
            return lighting.IsLit(spawnPoint)
                   || !IsAhead(spawnPoints.PositionOf(spawnPoint), characterPosition, walkingDirection, minMetresAhead);
        }

        /// <summary>
        /// Whether <paramref name="point"/> lies at least <paramref name="minMetresAhead"/> ahead of <paramref name="from"/>
        /// along <paramref name="direction"/>, measured on the ground.
        /// </summary>
        public static bool IsAhead(Vector3 point, Vector3 from, Vector3 direction, float minMetresAhead)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0f)
            {
                return true;
            }

            var offset = point - from;
            offset.y = 0f;
            return Vector3.Dot(offset, direction.normalized) >= minMetresAhead;
        }
    }
}
