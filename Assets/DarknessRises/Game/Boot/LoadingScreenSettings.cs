using System;
using UnityEngine;

namespace DarknessRises.Game.Boot
{
    /// <summary>
    /// How the loading screen looks and when its label comes up. It lives in the boot definition, not the Game
    /// Configuration: that is Level content, and this screen is what shows while Level content loads.
    /// </summary>
    [Serializable]
    public sealed class LoadingScreenSettings
    {
        [SerializeField, Tooltip("What the screen says once the wait has gone on. Dots are added after it.")]
        private string label = "LOADING";

        [SerializeField, Min(0f), Tooltip(
            "Seconds the screen stays plain black before the label comes up. Long enough that a change between Levels, " +
            "whose content is already on the device, never shows it.")]
        private float labelDelaySeconds = 1f;

        [SerializeField, Min(0.05f), Tooltip("Seconds each dot after the label takes to appear.")]
        private float dotSeconds = 0.4f;

        [SerializeField, Min(0), Tooltip("How many dots follow the label before they start over.")]
        private int maxDots = 3;

        [SerializeField, Min(1f)] private float fontSize = 48f;

        [SerializeField, Tooltip("Dim, like the rest of the game in the dark.")]
        private Color labelColor = new(0.8f, 0.8f, 0.8f, 0.85f);

        [SerializeField, Tooltip("What covers the screen.")]
        private Color coverColor = Color.black;

        [SerializeField, Tooltip(
            "Canvas sorting order. Above the HUD, the pause menu, the death screens, the screen fade and the debug menu, " +
            "so nothing of a scene that isn't ready shows through; below the version label.")]
        private int sortingOrder = 90;

        [Header("Download bar")]
        [SerializeField, Min(1f), Tooltip(
            "How wide the bar under the label is, in the HUD's reference pixels (1920 x 1080, scaled to the screen's height).")]
        private float barWidth = 520f;

        [SerializeField, Min(1f), Tooltip("How tall the bar is, in reference pixels: a thin line.")]
        private float barHeight = 4f;

        [SerializeField, Min(0f), Tooltip("The gap between the label's middle and the bar, in reference pixels.")]
        private float barOffset = 48f;

        [SerializeField, Tooltip("The part of the bar that is downloaded. Dim, like the label.")]
        private Color barFillColor = new(0.8f, 0.8f, 0.8f, 0.85f);

        [SerializeField, Tooltip("The part of the bar still to download: barely there against the black.")]
        private Color barTrackColor = new(0.8f, 0.8f, 0.8f, 0.15f);

        [SerializeField, Tooltip(
            "The sizes under the bar: {0} is the megabytes in, {1} the megabytes in all; ':1' gives one decimal. Empty shows none.")]
        private string sizesFormat = "{0:1} / {1:1} MB";

        [SerializeField, Min(1f), Tooltip("The sizes' font size.")]
        private float sizesFontSize = 24f;

        [SerializeField, Tooltip("The sizes' colour: dimmer than the label.")]
        private Color sizesColor = new(0.8f, 0.8f, 0.8f, 0.5f);

        [SerializeField, Min(0f), Tooltip("The gap between the bar and the sizes' middle, in reference pixels.")]
        private float sizesOffset = 32f;

        [SerializeField, Min(0.02f), Tooltip(
            "Seconds between two looks at how far the download has got. Each look costs Addressables a little garbage, so " +
            "it isn't every frame; a tenth of a second still reads as smooth.")]
        private float progressPollSeconds = 0.1f;

        public string Label => label;
        public float BarWidth => barWidth;
        public float BarHeight => barHeight;
        public float BarOffset => barOffset;
        public Color BarFillColor => barFillColor;
        public Color BarTrackColor => barTrackColor;
        public string SizesFormat => sizesFormat;
        public float SizesFontSize => sizesFontSize;
        public Color SizesColor => sizesColor;
        public float SizesOffset => sizesOffset;
        public float ProgressPollSeconds => progressPollSeconds;
        public float LabelDelaySeconds => labelDelaySeconds;
        public float DotSeconds => dotSeconds;
        public int MaxDots => maxDots;
        public float FontSize => fontSize;
        public Color LabelColor => labelColor;
        public Color CoverColor => coverColor;
        public int SortingOrder => sortingOrder;
    }
}
