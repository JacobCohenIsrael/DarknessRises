using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarknessRises.Game.Visibility
{
    /// <summary>
    /// The lights that can reveal an Enemy: Lightning, Gunfire (Muzzle flashes and lit Shots), Hit sparks, the Aura,
    /// the Flare and Grenade explosions. Whoever creates a light registers it once (pooled lights when they're created),
    /// and <see cref="Gather"/> reads which of them are shining right now.
    /// </summary>
    /// <remarks>
    /// An Enemy counts as visible while it's within the range of a shining point light, or while a directional light
    /// shines (a Lightning flash lights the whole Room). Obstacles and walls are ignored: a light reaches its full range.
    /// Owned by the scene's composition root; never global.
    /// </remarks>
    public sealed class SceneLights
    {
        private readonly List<Light> lights = new(64);

        public void Add(Light light)
        {
            if (light != null && !lights.Contains(light))
            {
                lights.Add(light);
            }
        }

        public void Remove(Light light)
        {
            lights.Remove(light);
        }

        /// <summary>Fills <paramref name="spots"/> with where the shining lights fall, and returns how many it filled.</summary>
        public int Gather(Span<LitSpot> spots)
        {
            var count = 0;
            for (var i = lights.Count - 1; i >= 0; i--)
            {
                var light = lights[i];
                if (light == null)
                {
                    // Destroyed with its scene or pool.
                    lights.RemoveAt(i);
                    continue;
                }

                if (count >= spots.Length || !light.isActiveAndEnabled || light.intensity <= 0f)
                {
                    continue;
                }

                spots[count++] = light.type == LightType.Directional
                    ? LitSpot.WholeRoom()
                    : LitSpot.Around(light.transform.position, light.range);
            }

            return count;
        }
    }
}
