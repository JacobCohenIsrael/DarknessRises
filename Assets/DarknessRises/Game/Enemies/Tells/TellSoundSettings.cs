using System;
using UnityEngine;

namespace DarknessRises.Game.Enemies.Tells
{
    /// <summary>
    /// The sounds a living Enemy makes now and then from where it is, so the player can hear where it is in Darkness.
    /// Played positionally from the Enemy, through its <see cref="EnemyAudioSettings.HearingRange"/>.
    /// </summary>
    [Serializable]
    public sealed class TellSoundSettings
    {
        [SerializeField, Tooltip("One of these is picked at random each time. Leave empty for a silent Enemy type.")]
        private AudioClip[] clips = new AudioClip[0];

        [SerializeField, Min(0f), Tooltip("Fewest seconds between two Tell sounds. The first one comes this randomly soon after spawning too.")]
        private float minIntervalSeconds = 2.5f;

        [SerializeField, Min(0f), Tooltip("Most seconds between two Tell sounds.")]
        private float maxIntervalSeconds = 6f;

        [SerializeField, Range(0.1f, 3f), Tooltip("Lowest pitch a Tell sound is played at.")]
        private float minPitch = 0.6f;

        [SerializeField, Range(0.1f, 3f), Tooltip("Highest pitch a Tell sound is played at.")]
        private float maxPitch = 0.8f;

        [SerializeField, Range(0f, 1f)]
        private float volume = 0.7f;

        public AudioClip[] Clips => clips;
        public float MinIntervalSeconds => minIntervalSeconds;
        public float MaxIntervalSeconds => maxIntervalSeconds;
        public float MinPitch => minPitch;
        public float MaxPitch => maxPitch;
        public float Volume => volume;
    }
}
