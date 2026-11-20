using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// The volume sliders: where each starts, how loudness follows them, the sound that tries out the Sound slider, and
    /// how their panel reads. The panel shows in the pause menu and in the Shop.
    /// </summary>
    [Serializable]
    public sealed class AudioVolumeSettings
    {
        [Header("Defaults")]
        [SerializeField, Range(VolumeCurve.MinPercent, VolumeCurve.MaxPercent), Tooltip("Where the Sound slider starts, in percent, until the player moves it. 100 plays the game's sounds as authored.")]
        private int soundDefaultPercent = 100;

        [SerializeField, Range(VolumeCurve.MinPercent, VolumeCurve.MaxPercent), Tooltip("Where the Ambient slider starts, in percent, until the player moves it. 100 plays the storm and Room ambience as authored.")]
        private int ambientDefaultPercent = 100;

        [SerializeField, Range(VolumeCurve.MinPercent, VolumeCurve.MaxPercent), Tooltip("Where the Music slider starts, in percent, until the player moves it.")]
        private int musicDefaultPercent = 80;

        [Header("Curve")]
        [SerializeField, Range(-80f, -6f), Tooltip("How loud a slider's lowest step (1 %) is, in decibels below full. Loudness rises evenly in decibels from there to 100 % (0 dB), so every step sounds like the same change; 0 % is silent. Lower makes the bottom of the slider quieter.")]
        private float quietestDecibels = -40f;

        [Header("Sample")]
        [SerializeField, Tooltip("Played when the player lets go of the Sound slider, so they hear the new volume. An approved placeholder already in the game.")]
        private AssetReferenceT<AudioClip> sampleClip;

        [SerializeField, Range(0f, 1f), Tooltip("The sample's own volume at a full Sound slider.")]
        private float sampleVolume = 0.8f;

        [SerializeField, Min(0f), Tooltip("Where in the clip the sample starts, in seconds.")]
        private float sampleClipStartSeconds;

        [Header("Wording")]
        [SerializeField, Tooltip("The button that opens the panel, in the pause menu and the Shop.")]
        private string openLabel = "SOUND";

        [SerializeField, Tooltip("The panel's title.")]
        private string title = "SOUND";

        [SerializeField, Tooltip("The Music slider's name.")]
        private string musicLabel = "MUSIC";

        [SerializeField, Tooltip("The Sound slider's name.")]
        private string soundLabel = "SOUND";

        [SerializeField, Tooltip("The Ambient slider's name.")]
        private string ambientLabel = "AMBIENT";

        [SerializeField, Tooltip("A slider's value. {0} is its percent.")]
        private string percentFormat = "{0}%";

        [SerializeField, Tooltip("The button that closes the panel.")]
        private string closeLabel = "BACK";

        public VolumeDefaults Defaults => new(soundDefaultPercent, ambientDefaultPercent, musicDefaultPercent);
        public VolumeCurve Curve => new(quietestDecibels);
        public AssetReferenceT<AudioClip> SampleClip => sampleClip;
        public float SampleVolume => sampleVolume;
        public float SampleClipStartSeconds => sampleClipStartSeconds;
        public string OpenLabel => openLabel;
        public string Title => title;
        public string PercentFormat => percentFormat;
        public string CloseLabel => closeLabel;

        /// <summary>The name of <paramref name="category"/>'s slider.</summary>
        public string LabelOf(AudioCategory category) => category switch
        {
            AudioCategory.Ambient => ambientLabel,
            AudioCategory.Music => musicLabel,
            _ => soundLabel,
        };
    }
}
