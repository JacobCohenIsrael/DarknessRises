using System;
using UnityEngine;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// How the build version label looks and where it sits. Sizes are in HUD units (the HUD's reference resolution is
    /// 1920 × 1080 and scales with screen height).
    /// </summary>
    [Serializable]
    public sealed class VersionLabelSettings
    {
        [SerializeField, Tooltip(
            "The corner of the safe area the label sits in. Bottom left is under the Skill names; the HUD uses the other corners.")]
        private ScreenCorner corner = ScreenCorner.BottomLeft;

        [SerializeField, Tooltip("From the corner of the safe area to the label, across and up (or down).")]
        private Vector2 margin = new(24f, 10f);

        [SerializeField, Min(1f), Tooltip("Small, so it never competes with the HUD.")]
        private float fontSize = 20f;

        [SerializeField, Tooltip("Low contrast in the dark: dim grey, mostly transparent.")]
        private Color color = new(0.7f, 0.7f, 0.7f, 0.35f);

        [SerializeField, Tooltip(
            "Canvas sorting order. Above the HUD, the pause menu, the death screens, the screen fade and the debug menu, so it shows " +
            "from boot through every Level.")]
        private int sortingOrder = 100;

        public ScreenCorner Corner => corner;
        public Vector2 Margin => margin;
        public float FontSize => fontSize;
        public Color Color => color;
        public int SortingOrder => sortingOrder;
    }
}
