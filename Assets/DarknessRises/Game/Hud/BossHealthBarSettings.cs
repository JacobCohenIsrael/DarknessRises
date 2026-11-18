using System;
using UnityEngine;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// The Boss health bar across the top of the HUD: its size, colours and how its trail and slide move. Sizes are in
    /// HUD units (the HUD's reference resolution is 1920 × 1080 and scales with screen height).
    /// </summary>
    [Serializable]
    public sealed class BossHealthBarSettings
    {
        [Header("Size")]
        [SerializeField, Min(1f), Tooltip("Widest the bar gets.")]
        private float maxWidth = 900f;

        [SerializeField, Min(0f), Tooltip(
            "Room kept clear on each side of the bar, inside the safe area, for the HP pips (top left) and the pause button (top right). " +
            "On narrow screens the bar gets shorter instead.")]
        private float sideClearance = 380f;

        [SerializeField, Min(0f), Tooltip("From the top of the safe area to the top of the name.")]
        private float topMargin = 36f;

        [SerializeField, Min(1f)] private float barHeight = 22f;

        [SerializeField, Min(1f)] private float nameFontSize = 34f;

        [SerializeField, Min(0f), Tooltip("Between the name and the bar.")]
        private float nameGap = 6f;

        [SerializeField, Min(1f), Tooltip("Width of the marks where phases 2 and 3 start.")]
        private float tickWidth = 4f;

        [Header("Colours")]
        [SerializeField] private Color nameColor = new(0.9f, 0.86f, 0.8f, 0.95f);
        [SerializeField] private Color backgroundColor = new(0.08f, 0.07f, 0.07f, 0.75f);
        [SerializeField] private Color fillColor = new(0.85f, 0.32f, 0.1f, 1f);

        [SerializeField, Tooltip("The hit points just lost, before the trail drains.")]
        private Color trailColor = new(1f, 0.85f, 0.6f, 0.95f);

        [SerializeField] private Color tickColor = new(0.9f, 0.86f, 0.8f, 0.85f);

        [Header("Movement")]
        [SerializeField, Min(0f), Tooltip("Seconds the trail stays put after a hit before it drains.")]
        private float trailHoldSeconds = 0.45f;

        [SerializeField, Min(0.01f), Tooltip("Share of the whole bar the trail drains per second.")]
        private float trailDrainPerSecond = 0.6f;

        [SerializeField, Min(0f), Tooltip("Seconds the bar takes to slide in when the fight starts, and out when it ends.")]
        private float slideSeconds = 0.4f;

        public float MaxWidth => maxWidth;
        public float SideClearance => sideClearance;
        public float TopMargin => topMargin;
        public float BarHeight => barHeight;
        public float NameFontSize => nameFontSize;
        public float NameGap => nameGap;
        public float TickWidth => tickWidth;
        public Color NameColor => nameColor;
        public Color BackgroundColor => backgroundColor;
        public Color FillColor => fillColor;
        public Color TrailColor => trailColor;
        public Color TickColor => tickColor;
        public float TrailHoldSeconds => trailHoldSeconds;
        public float TrailDrainPerSecond => trailDrainPerSecond;
        public float SlideSeconds => slideSeconds;

        /// <summary>Height of the whole bar, name included.</summary>
        public float TotalHeight => nameFontSize + nameGap + barHeight;
    }
}
