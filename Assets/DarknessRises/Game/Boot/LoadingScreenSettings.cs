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

        public string Label => label;
        public float LabelDelaySeconds => labelDelaySeconds;
        public float DotSeconds => dotSeconds;
        public int MaxDots => maxDots;
        public float FontSize => fontSize;
        public Color LabelColor => labelColor;
        public Color CoverColor => coverColor;
        public int SortingOrder => sortingOrder;
    }
}
