#if UNITY_EDITOR || DEBUG
using UnityEngine;

namespace DarknessRises.Game.Debugging
{
    /// <summary>
    /// Sizes and colours of the touch debug menu, in the HUD's reference units (1920x1080, scaled with screen height).
    /// Constants on purpose: the menu is a development tool compiled out of release builds, not gameplay, so it stays
    /// out of the authored configuration that ships.
    /// </summary>
    internal static class DebugMenuStyle
    {
        public static readonly Vector2 ReferenceResolution = new(1920f, 1080f);
        public const float MatchHeight = 1f;

        // Above the HUD, the pause menu and the death screens.
        public const int SortingOrder = 50;

        // Top right, just left of the pause button (48 from the edge, 96 wide), clear of the Boss health bar.
        public static readonly Vector2 DevButtonOffset = new(-172f, -40f);
        public static readonly Vector2 DevButtonSize = new(112f, 72f);
        public const float DevButtonFontSize = 30f;

        public static readonly Vector2 PanelSize = new(1040f, 860f);
        public const float PanelPadding = 36f;
        public const float RowSpacing = 22f;
        public const float ButtonSpacing = 18f;
        public const float RowHeight = 104f;
        public const float TitleFontSize = 52f;
        public const float LabelFontSize = 36f;

        public static readonly Color ShadeColor = new(0f, 0f, 0f, 0.8f);
        public static readonly Color PanelColor = new(0.1f, 0.1f, 0.11f, 0.97f);
        public static readonly Color ButtonColor = new(0.24f, 0.24f, 0.26f, 1f);
        public static readonly Color DevButtonColor = new(0.45f, 0.12f, 0.5f, 0.7f);
        public static readonly Color OnColor = new(0.2f, 0.55f, 0.3f, 1f);
        public static readonly Color TextColor = new(0.92f, 0.92f, 0.92f, 1f);
    }
}
#endif
