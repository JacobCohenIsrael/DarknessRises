using System;
using UnityEngine;

namespace DarknessRises.Game.Enemies
{
    /// <summary>How a dead Enemy leaves the Room: it lies still for a moment, then sinks into the floor and fades out.</summary>
    [Serializable]
    public sealed class EnemyDeathSettings
    {
        [SerializeField, Min(0f), Tooltip("Seconds the dead Enemy stays where it fell before it starts to sink.")]
        private float lingerSeconds = 0.5f;

        [SerializeField, Min(0.01f), Tooltip("Seconds it takes to sink and fade out completely.")]
        private float sinkSeconds = 1.6f;

        [SerializeField, Min(0f), Tooltip("Metres the dead Enemy sinks into the floor.")]
        private float sinkDepth = 1.2f;

        public float LingerSeconds => lingerSeconds;
        public float SinkSeconds => sinkSeconds;
        public float SinkDepth => sinkDepth;
    }
}
