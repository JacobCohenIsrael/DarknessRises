using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// How a Primary weapon's reload is shown and heard: a radial meter by the character that fills over the reload,
    /// a magazine-out sound when it starts and a magazine-in sound when it finishes. Shared by every Primary weapon,
    /// since each reloads the same way and only the reload time differs.
    /// </summary>
    [Serializable]
    public sealed class ReloadFeedbackSettings
    {
        [Header("Meter")]
        [SerializeField, Min(1f), Tooltip("Width and height of the meter, in HUD units (1080 tall).")]
        private float meterSize = 96f;

        [SerializeField, Tooltip("Where the meter sits relative to the character's feet, in world metres.")]
        private Vector3 worldOffset = new(0f, 2.4f, 0f);

        [SerializeField, Tooltip("The sweep that fills the disc clockwise from the top as the reload runs.")]
        private Color fillColor = new(1f, 0.78f, 0.32f, 0.95f);

        [SerializeField, Tooltip("The faint disc behind the sweep, showing how much is left. Light, so it reads in the dark. Clear for none.")]
        private Color backgroundColor = new(1f, 1f, 1f, 0.2f);

        [Header("Magazine out (reload starts)")]
        [SerializeField] private AssetReferenceT<AudioClip> magazineOutClip = new(string.Empty);

        [SerializeField, Range(0f, 1f)] private float magazineOutVolume = 0.6f;

        [SerializeField, Range(0.1f, 3f)] private float magazineOutPitch = 1.2f;

        [Header("Magazine in (reload finishes)")]
        [SerializeField] private AssetReferenceT<AudioClip> magazineInClip = new(string.Empty);

        [SerializeField, Range(0f, 1f)] private float magazineInVolume = 0.7f;

        [SerializeField, Range(0.1f, 3f)] private float magazineInPitch = 1.25f;

        public float MeterSize => meterSize;
        public Vector3 WorldOffset => worldOffset;
        public Color FillColor => fillColor;
        public Color BackgroundColor => backgroundColor;
        public AssetReferenceT<AudioClip> MagazineOutClip => magazineOutClip;
        public float MagazineOutVolume => magazineOutVolume;
        public float MagazineOutPitch => magazineOutPitch;
        public AssetReferenceT<AudioClip> MagazineInClip => magazineInClip;
        public float MagazineInVolume => magazineInVolume;
        public float MagazineInPitch => magazineInPitch;
    }
}
