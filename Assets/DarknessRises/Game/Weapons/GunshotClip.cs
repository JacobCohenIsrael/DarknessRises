using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Weapons
{
    /// <summary>One clip a Weapon's Gunshot can be, trimmed to the single shot it plays.</summary>
    [Serializable]
    public sealed class GunshotClip
    {
        [SerializeField, Tooltip("The clip. Loaded with the Level's content.")]
        private AssetReferenceT<AudioClip> clip = new(string.Empty);

        [SerializeField, Min(0f), Tooltip("Seconds into the clip the Gunshot starts, to skip anything before its shot, " +
                                          "such as the earlier shots of a recorded burst.")]
        private float startSeconds;

        [SerializeField, Min(0f), Tooltip("Seconds of the clip the Gunshot plays from its start, at pitch 1. It fades out over " +
                                          "its last moment. 0 plays to the clip's end.")]
        private float lengthSeconds;

        public AssetReferenceT<AudioClip> Clip => clip;
        public float StartSeconds => startSeconds;
        public float LengthSeconds => lengthSeconds;

        /// <summary>
        /// Where in a clip of <paramref name="clipLengthSeconds"/> the Gunshot starts, and how many clip seconds it plays,
        /// kept inside the clip.
        /// </summary>
        public void Trim(float clipLengthSeconds, out float start, out float length)
        {
            Trim(clipLengthSeconds, startSeconds, lengthSeconds, out start, out length);
        }

        /// <summary>
        /// Keeps a trim of <paramref name="trimLengthSeconds"/> from <paramref name="trimStartSeconds"/> (0 for the rest of
        /// the clip) inside a clip of <paramref name="clipLengthSeconds"/>.
        /// </summary>
        public static void Trim(float clipLengthSeconds, float trimStartSeconds, float trimLengthSeconds, out float start, out float length)
        {
            start = Mathf.Clamp(trimStartSeconds, 0f, clipLengthSeconds);
            var left = clipLengthSeconds - start;
            length = trimLengthSeconds > 0f ? Mathf.Min(trimLengthSeconds, left) : left;
        }
    }
}
