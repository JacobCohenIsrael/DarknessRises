using UnityEngine;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// Keeps its RectTransform over the screen's safe area, clear of notches and rounded corners, so the HUD placed in
    /// it stays readable and reachable. Sits directly under a Screen Space Overlay canvas that covers the screen.
    /// Only touches the layout when the safe area or the resolution changes.
    /// </summary>
    public sealed class SafeAreaFrame : MonoBehaviour
    {
        [SerializeField, Tooltip("This frame, a direct child of the HUD canvas.")]
        private RectTransform frame;

        private Rect fittedArea;
        private Vector2Int fittedScreen;

        private void OnEnable()
        {
            fittedScreen = Vector2Int.zero;
            Fit();
        }

        private void Update()
        {
            Fit();
        }

        private void Fit()
        {
            if (frame == null)
            {
                return;
            }

            var area = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (area == fittedArea && screen == fittedScreen || screen.x <= 0 || screen.y <= 0)
            {
                return;
            }

            fittedArea = area;
            fittedScreen = screen;
            frame.anchorMin = new Vector2(area.xMin / screen.x, area.yMin / screen.y);
            frame.anchorMax = new Vector2(area.xMax / screen.x, area.yMax / screen.y);
            frame.offsetMin = frame.offsetMax = Vector2.zero;
        }
    }
}
