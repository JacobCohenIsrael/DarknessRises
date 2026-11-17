using System;
using UnityEngine;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// How the Rumble sounds: a stretch of a clip that swells into the flash, then fades away.
    /// </summary>
    [Serializable]
    public sealed class RumbleSettings
    {
        [SerializeField] private AudioClip clip;

        [SerializeField, Range(0f, 1f)] private float volume = 0.9f;

        [SerializeField, Min(0f), Tooltip("Seconds into the clip where the Rumble starts. Pick a spot that swells into the flash.")]
        private float clipStartSeconds = 8.3f;

        [SerializeField, Min(0f), Tooltip("Seconds the Rumble takes to fade out once the flash has struck.")]
        private float fadeOutSeconds = 1.6f;

        public AudioClip Clip => clip;
        public float Volume => volume;
        public float ClipStartSeconds => clipStartSeconds;
        public float FadeOutSeconds => fadeOutSeconds;
    }
}
