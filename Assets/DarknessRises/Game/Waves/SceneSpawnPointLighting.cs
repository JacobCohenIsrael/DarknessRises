using System.Collections.Generic;
using UnityEngine;

namespace DarknessRises.Game.Waves
{
    /// <summary>
    /// Checks the scene's lights against a Room's Spawn points: a Spawn point is lit when a switched-on light
    /// at or above the authored intensity reaches it (Lightning, the Aura, Muzzle flashes, hit sparks, Skills).
    /// </summary>
    /// <remarks>
    /// The scene's lights are gathered with <see cref="Refresh"/>, once per Station, because pooled effect lights
    /// are created at runtime. Checking a Spawn point never allocates.
    /// </remarks>
    public sealed class SceneSpawnPointLighting : ISpawnPointLighting
    {
        private readonly RoomSpawnPoints spawnPoints;
        private readonly float litIntensity;
        private readonly List<Light> lights = new();

        public SceneSpawnPointLighting(RoomSpawnPoints spawnPoints, float litIntensity)
        {
            this.spawnPoints = spawnPoints;
            this.litIntensity = litIntensity;
        }

        /// <summary>Gathers every light in the scene, switched on or not, so lights that switch on later count too.</summary>
        public void Refresh()
        {
            lights.Clear();
            lights.AddRange(Object.FindObjectsByType<Light>(FindObjectsInactive.Include));
        }

        public bool IsLit(int spawnPoint)
        {
            var point = spawnPoints.PositionOf(spawnPoint);
            for (var i = 0; i < lights.Count; i++)
            {
                var light = lights[i];
                if (light == null || !light.isActiveAndEnabled || light.intensity < litIntensity)
                {
                    continue;
                }

                var lightTransform = light.transform;
                if (LightReach.Covers(
                        light.type,
                        lightTransform.position,
                        lightTransform.forward,
                        light.range,
                        light.spotAngle,
                        point))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
