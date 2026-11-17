using System;
using UnityEngine;

namespace DarknessRises.Game.Enemies
{
    /// <summary>The sounds an Enemy type makes. Played positionally from the Enemy. Any clip may be left empty.</summary>
    [Serializable]
    public sealed class EnemyAudioSettings
    {
        [SerializeField, Tooltip("Played when the Enemy enters through its Spawn point.")]
        private AudioClip spawnClip;

        [SerializeField, Tooltip("Played when a hit hurts the Enemy without killing it.")]
        private AudioClip hurtClip;

        [SerializeField, Tooltip("Played when the Enemy dies.")]
        private AudioClip deathClip;

        [SerializeField, Range(0f, 1f)]
        private float volume = 0.8f;

        [SerializeField, Min(0.1f), Tooltip("Metres beyond which the Enemy can't be heard.")]
        private float hearingRange = 25f;

        public AudioClip SpawnClip => spawnClip;
        public AudioClip HurtClip => hurtClip;
        public AudioClip DeathClip => deathClip;
        public float Volume => volume;
        public float HearingRange => hearingRange;
    }
}
