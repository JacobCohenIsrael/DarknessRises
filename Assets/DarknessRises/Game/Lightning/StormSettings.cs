using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// How the storm sounds: a thunder loop that plays all the time Lightning reaches the character (the storm bed), and
    /// the Rumble, the swell it gives after each flash.
    /// </summary>
    [Serializable]
    public sealed class StormSettings
    {
        [SerializeField, Tooltip("The thunder loop. It plays on a loop, never cut, and swells after each flash.")]
        private AssetReferenceT<AudioClip> clip = new(string.Empty);

        [Header("Storm bed")]
        [SerializeField, Range(0f, 1f), Tooltip("The bed's volume between Rumbles.")]
        private float baseVolume = 0.5f;

        [SerializeField, Min(0f), Tooltip("Seconds the bed takes to fade in from silence, on entering a Room with windows.")]
        private float fadeInSeconds = 1.5f;

        [SerializeField, Min(0f), Tooltip("Seconds the bed takes to fade out to silence, in a Room without windows or with " +
                                          "Lightning switched off.")]
        private float fadeOutSeconds = 1.5f;

        [Header("Rumble (the swell after each flash)")]
        [SerializeField, Range(0f, 1f), Tooltip("The bed's volume at the top of a Rumble. At least the base volume.")]
        private float swellPeakVolume = 0.95f;

        [SerializeField, Min(0f), Tooltip("Seconds a Rumble takes to rise from the current volume to the peak.")]
        private float swellAttackSeconds = 0.25f;

        [SerializeField, Min(0f), Tooltip("Seconds a Rumble holds at the peak.")]
        private float swellHoldSeconds = 0.5f;

        [SerializeField, Min(0f), Tooltip("Seconds a Rumble takes to settle back to the base volume.")]
        private float swellReleaseSeconds = 1.8f;

        [SerializeField, Min(0.1f), Tooltip("Lowest pitch a Rumble nudges the bed to, eased back afterwards. Lower sounds " +
                                            "farther and heavier.")]
        private float minSwellPitch = 0.94f;

        [SerializeField, Min(0.1f), Tooltip("Highest pitch a Rumble nudges the bed to, eased back afterwards.")]
        private float maxSwellPitch = 1.06f;

        [Header("Between Levels")]
        [SerializeField, Min(0f), Tooltip("Seconds the bed takes to fade to silence once the Level ends (the calm before the " +
                                          "far door) and on the way to the Shop, so no storm rolls over the Level change.")]
        private float hushFadeOutSeconds = 2.5f;

        [SerializeField, Min(0f), Tooltip("Seconds the bed takes to fade back in once the next Level plays. Gentler than a " +
                                          "Room's fade in, so the storm creeps back rather than arriving.")]
        private float hushFadeInSeconds = 4f;

        public AssetReferenceT<AudioClip> Clip => clip;

        public StormEnvelope Envelope => new(
            baseVolume,
            fadeInSeconds,
            fadeOutSeconds,
            swellPeakVolume,
            swellAttackSeconds,
            swellHoldSeconds,
            swellReleaseSeconds,
            minSwellPitch,
            maxSwellPitch,
            hushFadeOutSeconds,
            hushFadeInSeconds);

        /// <summary>Keeps every range the right way round while it's edited.</summary>
        public void Validate()
        {
            swellPeakVolume = Mathf.Max(swellPeakVolume, baseVolume);
            maxSwellPitch = Mathf.Max(maxSwellPitch, minSwellPitch);
        }
    }
}
