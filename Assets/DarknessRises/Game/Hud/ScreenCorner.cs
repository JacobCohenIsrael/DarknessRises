using UnityEngine;

namespace DarknessRises.Game.Hud
{
    /// <summary>A corner of the screen's safe area.</summary>
    public enum ScreenCorner
    {
        BottomLeft,
        BottomRight,
        TopLeft,
        TopRight,
    }

    public static class ScreenCornerExtensions
    {
        /// <summary>The corner as a normalized point, for a RectTransform's anchors and pivot: (0, 0) is bottom left.</summary>
        public static Vector2 ToAnchor(this ScreenCorner corner)
        {
            return corner switch
            {
                ScreenCorner.BottomRight => new Vector2(1f, 0f),
                ScreenCorner.TopLeft => new Vector2(0f, 1f),
                ScreenCorner.TopRight => new Vector2(1f, 1f),
                _ => Vector2.zero,
            };
        }
    }
}
