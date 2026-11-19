using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// The sound of a Weapon that fires continuously: one loop for as long as it fires, instead of a Gunshot per Shot.
    /// Times are in clip seconds; the loop plays at pitch 1.
    /// </summary>
    [Serializable]
    public sealed class FiringLoopSettings
    {
        [SerializeField, Tooltip("The clip the loop plays from. Empty for a Weapon with a Gunshot per Shot.")]
        private AssetReferenceT<AudioClip> clip = new(string.Empty);

        [SerializeField, Range(0f, 1f), Tooltip("Volume of the loop.")]
        private float volume = 0.7f;

        [SerializeField, Min(0f), Tooltip("Seconds into the clip the sound starts when firing starts: late in its build-up, " +
                                          "so firing is heard at once.")]
        private float startSeconds;

        [SerializeField, Min(0f), Tooltip("Seconds into the clip the steady part that repeats starts.")]
        private float loopStartSeconds;

        [SerializeField, Min(0f), Tooltip("Seconds into the clip the steady part that repeats ends, before the clip dies away.")]
        private float loopEndSeconds;

        [SerializeField, Min(0f), Tooltip("Seconds each pass of the steady part crossfades into the next, so the repeat isn't heard.")]
        private float crossfadeSeconds = 0.3f;

        [SerializeField, Min(0f), Tooltip("Seconds the sound takes to fade in when firing starts. Short: just enough not to click.")]
        private float fadeInSeconds = 0.04f;

        [SerializeField, Min(0f), Tooltip("Seconds the sound takes to fade out when firing stops.")]
        private float fadeOutSeconds = 0.35f;

        [SerializeField, Min(0f), Tooltip("Seconds without a Shot after which the sound fades out by itself, as when the " +
                                          "Weapon is put away. Longer than the gap between two Shots.")]
        private float stopAfterSeconds = 0.2f;

        public AssetReferenceT<AudioClip> Clip => clip;
        public float Volume => volume;

        /// <summary>The loop's timing in a clip of <paramref name="clipLengthSeconds"/>.</summary>
        public FiringLoopTiming Timing(float clipLengthSeconds)
        {
            return new FiringLoopTiming(clipLengthSeconds, startSeconds, loopStartSeconds, loopEndSeconds, crossfadeSeconds,
                fadeInSeconds, fadeOutSeconds, stopAfterSeconds);
        }
    }
}
