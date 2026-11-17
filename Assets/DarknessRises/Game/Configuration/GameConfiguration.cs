using DarknessRises.Game.Cameras;
using DarknessRises.Game.Player;
using UnityEngine;

namespace DarknessRises.Game.Configuration
{
    /// <summary>
    /// The root configuration asset. Every authored definition the game reads is reachable from here,
    /// so tuning a number never needs a code change (ADR 0003). Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Game Configuration", fileName = "GameConfiguration", order = 0)]
    public sealed class GameConfiguration : ScriptableObject
    {
        [SerializeField] private PlayerDefinition player;
        [SerializeField] private CameraDefinition fixedAngleCamera;

        public PlayerDefinition Player => player;
        public CameraDefinition FixedAngleCamera => fixedAngleCamera;
    }
}
