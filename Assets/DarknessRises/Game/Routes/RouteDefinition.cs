using UnityEngine;

namespace DarknessRises.Game.Routes
{
    /// <summary>
    /// Authored tuning for walking the Route. The Route's shape and its Stations are authored in the scene;
    /// the walking speed comes from the player definition.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Route Definition", fileName = "RouteDefinition", order = 30)]
    public sealed class RouteDefinition : ScriptableObject
    {
        [SerializeField, Min(0f), Tooltip(
            "Until Stations have Waves, seconds the character waits at a Station before it counts as cleared. " +
            "0 clears it at once.")]
        private float placeholderStationHoldSeconds = 1.5f;

        public float PlaceholderStationHoldSeconds => placeholderStationHoldSeconds;
    }
}
