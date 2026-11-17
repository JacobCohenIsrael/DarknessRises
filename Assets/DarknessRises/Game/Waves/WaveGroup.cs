using System;
using DarknessRises.Game.Enemies;
using UnityEngine;

namespace DarknessRises.Game.Waves
{
    /// <summary>Some Enemies of one Enemy type in a Wave.</summary>
    [Serializable]
    public sealed class WaveGroup
    {
        [SerializeField] private EnemyDefinition enemyType;

        [SerializeField, Min(0)] private int count = 1;

        public EnemyDefinition EnemyType => enemyType;
        public int Count => count;
    }
}
