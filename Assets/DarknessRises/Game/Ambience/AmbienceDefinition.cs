using DarknessRises.Game.Randomness;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Ambience
{
    /// <summary>
    /// Authored tuning for the ambient bed: now and then a quiet, positional sound from the dark (bats in the rafters),
    /// a few seconds cut from a long recording, from somewhere around the character up near the ceiling, fading in and
    /// out. Background, not a Tell: it never comes from an Enemy or a Critter. Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Ambience Definition", fileName = "AmbienceDefinition", order = 47)]
    public sealed class AmbienceDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Long recordings a bed is cut from, one picked at random each time. Leave empty for no ambient bed.")]
        private AssetReferenceT<AudioClip>[] clips = new AssetReferenceT<AudioClip>[0];

        [Header("When")]
        [SerializeField, Min(0f), Tooltip("Fewest seconds of silence between two beds.")]
        private float minGapSeconds = 20f;

        [SerializeField, Min(0f), Tooltip("Most seconds of silence between two beds.")]
        private float maxGapSeconds = 45f;

        [SerializeField, Min(0f), Tooltip("Seconds from the Level's start before the first bed.")]
        private float firstDelaySeconds = 8f;

        [Header("How long")]
        [SerializeField, Min(0f), Tooltip("Shortest bed, in clip seconds.")]
        private float minSliceSeconds = 6f;

        [SerializeField, Min(0f), Tooltip("Longest bed, in clip seconds.")]
        private float maxSliceSeconds = 12f;

        [SerializeField, Min(0f), Tooltip("Seconds a bed fades in over, and out over, so it seeps in rather than starts.")]
        private float fadeSeconds = 2.5f;

        [SerializeField, Min(0f), Tooltip("Seconds a bed fades out over when a Boss fight starts or the character dies.")]
        private float cutFadeSeconds = 1f;

        [Header("Where and how loud")]
        [SerializeField, Min(0f), Tooltip("Fewest metres from the character a bed comes from: out in the dark.")]
        private float minMetres = 10f;

        [SerializeField, Min(0f), Tooltip("Most metres from the character a bed comes from.")]
        private float maxMetres = 16f;

        [SerializeField, Tooltip("Metres above the floor a bed comes from: up near the ceiling.")]
        private float heightMetres = 3.5f;

        [SerializeField, Range(0f, 1f), Tooltip("Volume of a bed at its source. Quiet: it's background.")]
        private float volume = 0.2f;

        [SerializeField, Range(0.1f, 3f), Tooltip("Lowest pitch of a bed.")]
        private float minPitch = 0.95f;

        [SerializeField, Range(0.1f, 3f), Tooltip("Highest pitch of a bed.")]
        private float maxPitch = 1.05f;

        [SerializeField, Min(0.1f), Tooltip("Metres beyond which a bed can't be heard.")]
        private float hearingRange = 30f;

        public AssetReferenceT<AudioClip>[] Clips => clips;
        public float MinSliceSeconds => minSliceSeconds;
        public float MaxSliceSeconds => maxSliceSeconds;
        public float FadeSeconds => fadeSeconds;
        public float CutFadeSeconds => cutFadeSeconds;
        public float MinMetres => minMetres;
        public float MaxMetres => maxMetres;
        public float HeightMetres => heightMetres;
        public float Volume => volume;
        public float MinPitch => minPitch;
        public float MaxPitch => maxPitch;
        public float HearingRange => hearingRange;

        public AmbientBedSchedule ToSchedule(IRandom random)
        {
            return new AmbientBedSchedule(minGapSeconds, maxGapSeconds, firstDelaySeconds, random);
        }
    }
}
