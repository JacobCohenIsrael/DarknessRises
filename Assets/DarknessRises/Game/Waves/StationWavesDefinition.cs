using System;
using System.Collections.Generic;
using DarknessRises.Game.Enemies;
using UnityEngine;

namespace DarknessRises.Game.Waves
{
    /// <summary>The Waves fought at one Station, in order. The Station is cleared once the last one is dead.</summary>
    [Serializable]
    public sealed class StationWavesDefinition
    {
        [SerializeField] private WaveDefinition[] waves = Array.Empty<WaveDefinition>();

        public IReadOnlyList<WaveDefinition> Waves => waves;

        public WavePlan<EnemyDefinition>[] ToPlans()
        {
            var plans = new WavePlan<EnemyDefinition>[waves.Length];
            for (var i = 0; i < waves.Length; i++)
            {
                plans[i] = waves[i].ToPlan();
            }

            return plans;
        }
    }
}
