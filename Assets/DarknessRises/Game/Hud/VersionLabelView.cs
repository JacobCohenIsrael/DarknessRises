using TMPro;
using UnityEngine;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// The build version in a corner of the screen's safe area, on its own canvas in the boot scene so it shows from boot
    /// through every Level. The text is set once. The canvas has no raycaster and the text isn't a raycast target, so it
    /// never blocks a tap, a Gesture or the Grenade button.
    /// </summary>
    public sealed class VersionLabelView : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private TMP_Text label;

        private Rect fittedSafeArea;

        /// <summary>Lays the label out by <paramref name="settings"/> and shows <paramref name="text"/>.</summary>
        public void Show(VersionLabelSettings settings, string text)
        {
            canvas.sortingOrder = settings.SortingOrder;

            var anchor = settings.Corner.ToAnchor();
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = new Vector2(
                anchor.x > 0f ? -settings.Margin.x : settings.Margin.x,
                anchor.y > 0f ? -settings.Margin.y : settings.Margin.y);

            label.fontSize = settings.FontSize;
            label.color = settings.Color;
            label.alignment = anchor.x > 0f ? TextAlignmentOptions.BottomRight : TextAlignmentOptions.BottomLeft;
            label.raycastTarget = false;
            label.text = text;
            FitSafeArea();
        }

        private void Update()
        {
            // A rotation or a resized browser window can move the safe area.
            FitSafeArea();
        }

        private void FitSafeArea()
        {
            var area = Screen.safeArea;
            if (area == fittedSafeArea || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            fittedSafeArea = area;
            safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        }
    }
}
