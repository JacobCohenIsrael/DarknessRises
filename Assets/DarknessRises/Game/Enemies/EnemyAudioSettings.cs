using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;

namespace DarknessRises.Game.Enemies
{
    /// <summary>The sounds an Enemy type makes. Played positionally from the Enemy. Any clip may be left empty.</summary>
    [Serializable]
    public sealed class EnemyAudioSettings
    {
        [SerializeField, Tooltip("Played when the Enemy enters through its Spawn point.")]
        private AssetReferenceT<AudioClip> spawnClip = new(string.Empty);

        [SerializeField, Tooltip("Played when a hit hurts the Enemy without killing it.")]
        private AssetReferenceT<AudioClip> hurtClip = new(string.Empty);

        [SerializeField, Tooltip("Played when the Enemy dies.")]
        private AssetReferenceT<AudioClip> deathClip = new(string.Empty);

        [SerializeField, Tooltip("Played when the Enemy starts its Wind-up: the audible half of the warning.")]
        private AssetReferenceT<AudioClip> windUpClip = new(string.Empty);

        [SerializeField, Tooltip("Played when the Wind-up ends and the Enemy strikes, whether or not it lands.")]
        private AssetReferenceT<AudioClip> attackClip = new(string.Empty);

        [SerializeField, Tooltip("Mixer group the Enemy's sounds go through. Its pitch follows the Enemy time scale, so Slow-mo slows them.")]
        private AudioMixerGroup outputGroup;

        [SerializeField, Range(0f, 1f)]
        private float volume = 0.8f;

        [SerializeField, Min(0.1f), Tooltip("Metres beyond which the Enemy can't be heard.")]
        private float hearingRange = 25f;

        public AssetReferenceT<AudioClip> SpawnClip => spawnClip;
        public AssetReferenceT<AudioClip> HurtClip => hurtClip;
        public AssetReferenceT<AudioClip> DeathClip => deathClip;
        public AssetReferenceT<AudioClip> WindUpClip => windUpClip;
        public AssetReferenceT<AudioClip> AttackClip => attackClip;
        public AudioMixerGroup OutputGroup => outputGroup;
        public float Volume => volume;
        public float HearingRange => hearingRange;
    }
}
