using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// How the Rumble (the thunder after a flash) sounds: a stretch of a clip at a varied pitch and start point, held a
    /// moment and then fading away.
    /// </summary>
    [Serializable]
    public sealed class RumbleSettings
    {
        [SerializeField] private AssetReferenceT<AudioClip> clip = new(string.Empty);

        [SerializeField, Range(0f, 1f)] private float volume = 0.9f;

        [SerializeField, Min(0.1f), Tooltip("Lowest pitch a Rumble plays at. Lower sounds farther and heavier.")]
        private float minPitch = 0.85f;

        [SerializeField, Min(0.1f), Tooltip("Highest pitch a Rumble plays at.")]
        private float maxPitch = 1.15f;

        [SerializeField, Min(0f), Tooltip("Earliest point in the clip, in seconds, where a Rumble may start.")]
        private float minClipStartSeconds = 0.4f;

        [SerializeField, Min(0f), Tooltip("Latest point in the clip, in seconds, where a Rumble may start. Leave enough clip " +
                                          "after it for the hold and the fade.")]
        private float maxClipStartSeconds = 24f;

        [SerializeField, Min(0f), Tooltip("Seconds a Rumble plays at full volume before it starts to fade.")]
        private float holdSeconds = 1f;

        [SerializeField, Min(0f), Tooltip("Seconds the thunder rolls on, fading out, after its hold.")]
        private float fadeOutSeconds = 3.5f;

        [SerializeField, Min(0f), Tooltip("Seconds a Rumble takes to fade out when it's cut short (leaving the Room, " +
                                          "Lightning switched off), so it never stops dead.")]
        private float cutFadeSeconds = 0.6f;

        public AssetReferenceT<AudioClip> Clip => clip;
        public float Volume => volume;
        public RumbleVariation Variation => new(minPitch, maxPitch, minClipStartSeconds, maxClipStartSeconds);
        public float HoldSeconds => holdSeconds;
        public float FadeOutSeconds => fadeOutSeconds;
        public float CutFadeSeconds => cutFadeSeconds;

        /// <summary>Keeps every range the right way round while it's edited.</summary>
        public void Validate()
        {
            maxPitch = Mathf.Max(maxPitch, minPitch);
            maxClipStartSeconds = Mathf.Max(maxClipStartSeconds, minClipStartSeconds);
        }
    }
}
