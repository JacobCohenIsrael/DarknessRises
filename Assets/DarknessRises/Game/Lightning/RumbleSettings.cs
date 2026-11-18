using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// How the Rumble sounds: a stretch of a clip that swells into the flash, then fades away.
    /// </summary>
    [Serializable]
    public sealed class RumbleSettings
    {
        [SerializeField] private AssetReferenceT<AudioClip> clip = new(string.Empty);

        [SerializeField, Range(0f, 1f)] private float volume = 0.9f;

        [SerializeField, Min(0f), Tooltip("Seconds into the clip where the Rumble starts. Pick a spot that swells into the flash.")]
        private float clipStartSeconds = 8.3f;

        [SerializeField, Min(0f), Tooltip("Seconds the thunder rolls on, fading out, once the flash has struck.")]
        private float fadeOutSeconds = 3.5f;

        [SerializeField, Min(0f), Tooltip("Seconds a Rumble takes to fade out when it's cut short (leaving the Room, " +
                                          "Lightning switched off), so it never stops dead.")]
        private float cutFadeSeconds = 0.6f;

        public AssetReferenceT<AudioClip> Clip => clip;
        public float Volume => volume;
        public float ClipStartSeconds => clipStartSeconds;
        public float FadeOutSeconds => fadeOutSeconds;
        public float CutFadeSeconds => cutFadeSeconds;
    }
}
