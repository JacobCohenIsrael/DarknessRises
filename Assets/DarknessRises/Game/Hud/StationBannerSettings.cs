using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Hud
{
    /// <summary>One banner of the Station rhythm: what it says, its colour and the sound that comes with it.</summary>
    [Serializable]
    public sealed class StationBannerCue
    {
        [SerializeField, Tooltip("The banner's text. Short and loud.")]
        private string text;

        [SerializeField] private Color color = Color.white;

        [SerializeField, Tooltip("Played once, in 2D, as the banner pops in. None: silent.")]
        private AssetReferenceT<AudioClip> clip = new(string.Empty);

        [SerializeField, Min(0f), Tooltip("Seconds into the clip the sound starts from, to skip a slow or silent opening.")]
        private float clipStartSeconds;

        [SerializeField, Range(0f, 1f)] private float volume = 0.8f;

        public StationBannerCue()
        {
        }

        public StationBannerCue(string text, Color color)
        {
            this.text = text;
            this.color = color;
        }

        public string Text => text;
        public Color Color => color;
        public AssetReferenceT<AudioClip> Clip => clip;
        public float ClipStartSeconds => clipStartSeconds;
        public float Volume => volume;
    }

    /// <summary>
    /// The banners of the Station rhythm: the Ready banner as the character stops at a Station, the Clear banner once its
    /// last Wave is dead, and the Go cue that points along the Route before the character sets off. Sizes are in HUD
    /// units (the HUD's reference resolution is 1920 × 1080 and scales with screen height).
    /// </summary>
    [Serializable]
    public sealed class StationBannerSettings
    {
        [Header("Banners")]
        [SerializeField, Tooltip("Shown while the character plants at a Station, before its first Wave.")]
        private StationBannerCue ready = new("GET READY", new Color(1f, 0.72f, 0.2f, 1f));

        [SerializeField, Tooltip("Shown once the Station's last Wave is dead.")]
        private StationBannerCue clear = new("CLEAR", new Color(0.55f, 1f, 0.6f, 1f));

        [SerializeField, Tooltip("Shown with an arrow pointing along the Route, just before the character sets off.")]
        private StationBannerCue go = new("GO", new Color(1f, 0.95f, 0.35f, 1f));

        [Header("Look")]
        [SerializeField, Min(1f)] private float fontSize = 150f;

        [SerializeField, Tooltip("Extra space between the letters, in hundredths of the font size.")]
        private float characterSpacing = 14f;

        [SerializeField, Tooltip("From the centre of the screen to the centre of the banner. Above the middle, clear of the character.")]
        private Vector2 position = new(0f, 250f);

        [SerializeField, Tooltip("The strip across the screen behind the text, so it reads over a Room lit by Lightning. Clear for none.")]
        private Color bandColor = new(0f, 0f, 0f, 0.45f);

        [SerializeField, Min(0f), Tooltip("Height of the strip behind the text.")]
        private float bandHeight = 210f;

        [Header("Pop")]
        [SerializeField, Min(0f), Tooltip("Seconds a banner takes to pop in: it fades in while it shrinks to its size.")]
        private float popSeconds = 0.16f;

        [SerializeField, Min(1f), Tooltip("How many times its size a banner starts at when it pops in.")]
        private float popScale = 1.7f;

        [SerializeField, Min(0f), Tooltip("Seconds a banner takes to fade out when its beat ends.")]
        private float fadeOutSeconds = 0.25f;

        [SerializeField, Min(0f), Tooltip("Seconds the Go cue stays on after the character has set off, before it fades.")]
        private float goLingerSeconds = 0.5f;

        [Header("Go arrow")]
        [SerializeField, Min(1f), Tooltip("Width and height of the arrow.")]
        private float arrowSize = 130f;

        [SerializeField, Min(0f), Tooltip("From the centre of the Go text to the arrow, along the Route's direction on screen.")]
        private float arrowDistance = 250f;

        [SerializeField, Min(0f), Tooltip("How far the arrow nudges along its direction and back.")]
        private float arrowPulseDistance = 36f;

        [SerializeField, Min(0f), Tooltip("Nudges per second.")]
        private float arrowPulsesPerSecond = 3f;

        public StationBannerCue Ready => ready;
        public StationBannerCue Clear => clear;
        public StationBannerCue Go => go;
        public float FontSize => fontSize;
        public float CharacterSpacing => characterSpacing;
        public Vector2 Position => position;
        public Color BandColor => bandColor;
        public float BandHeight => bandHeight;
        public float PopSeconds => popSeconds;
        public float PopScale => popScale;
        public float FadeOutSeconds => fadeOutSeconds;
        public float GoLingerSeconds => goLingerSeconds;
        public float ArrowSize => arrowSize;
        public float ArrowDistance => arrowDistance;
        public float ArrowPulseDistance => arrowPulseDistance;
        public float ArrowPulsesPerSecond => arrowPulsesPerSecond;
    }
}
