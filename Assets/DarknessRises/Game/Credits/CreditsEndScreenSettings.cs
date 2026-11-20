using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Credits
{
    /// <summary>
    /// How the end screen shows Credits: the Run's earnings and the new total counted up arcade-style, and the bonus of
    /// the end-of-Run ad landing on top.
    /// </summary>
    [Serializable]
    public sealed class CreditsEndScreenSettings
    {
        [SerializeField, Tooltip("Credits have their own colour everywhere, so they never read as the Score.")]
        private Color color = new(0.35f, 1f, 0.78f, 1f);

        [SerializeField, Min(0f), Tooltip("Seconds the Credits earned and the total take to count up. Real time: the Level behind the screen may be frozen.")]
        private float countUpSeconds = 1.2f;

        [SerializeField, Min(0f), Tooltip("Seconds the ad bonus takes to count up, after the ad.")]
        private float bonusCountUpSeconds = 1f;

        [SerializeField, Min(1f), Tooltip("How many times its size the total jumps to when the ad bonus lands.")]
        private float bonusKickScale = 1.35f;

        [SerializeField, Min(0f), Tooltip("Seconds that jump takes to settle back.")]
        private float bonusKickSeconds = 0.25f;

        [SerializeField, Tooltip("Played when the ad bonus lands. Empty for no sound.")]
        private AssetReferenceT<AudioClip> bonusClip = new(string.Empty);

        [SerializeField, Min(0f), Tooltip("Seconds into the clip where the sound starts, to skip a quiet lead-in.")]
        private float bonusClipStartSeconds;

        [SerializeField, Range(0f, 1f)] private float bonusVolume = 0.8f;

        public Color Color => color;
        public float CountUpSeconds => countUpSeconds;
        public float BonusCountUpSeconds => bonusCountUpSeconds;
        public float BonusKickScale => bonusKickScale;
        public float BonusKickSeconds => bonusKickSeconds;
        public AssetReferenceT<AudioClip> BonusClip => bonusClip;
        public float BonusClipStartSeconds => bonusClipStartSeconds;
        public float BonusVolume => bonusVolume;
    }
}
