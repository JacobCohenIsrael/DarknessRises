using System;
using System.Collections.Generic;
using DarknessRises.Game.Waves;
using UnityEngine;

namespace DarknessRises.Game.Routes
{
    /// <summary>
    /// Authored data for the Route: the Waves fought at each Station, and when a Spawn point counts as lit.
    /// The Route's shape, its Stations and the Rooms' Spawn points are authored in the scene;
    /// the walking speed comes from the player definition.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Route Definition", fileName = "RouteDefinition", order = 30)]
    public sealed class RouteDefinition : ScriptableObject
    {
        [SerializeField, Tooltip(
            "Each Station's Waves, in the order the Route reaches the Stations. " +
            "A Station without an entry here is cleared as soon as the character reaches it.")]
        private StationWavesDefinition[] stations = Array.Empty<StationWavesDefinition>();

        [SerializeField, Min(0f), Tooltip(
            "A light at or above this intensity lights the Spawn points within its reach, so no Enemy enters there. " +
            "Lights that are off never count.")]
        private float spawnPointLitIntensity = 0.05f;

        public IReadOnlyList<StationWavesDefinition> Stations => stations;
        public float SpawnPointLitIntensity => spawnPointLitIntensity;
    }
}
