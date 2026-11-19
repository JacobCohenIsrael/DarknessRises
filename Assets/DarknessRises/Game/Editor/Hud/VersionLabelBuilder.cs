using DarknessRises.Game.Hud;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Editor.Hud
{
    /// <summary>
    /// Builds the build version label in the boot scene: its own overlay canvas, scaled like the HUD, with no raycaster so
    /// it never takes a tap, and a rect fitted to the safe area that holds the text. Its look comes from the
    /// <see cref="VersionLabelSettings"/> at runtime.
    /// </summary>
    public static class VersionLabelBuilder
    {
        // Wide and tall enough for "v99.99.99-dev" at any authored size up to a heading; the text doesn't wrap anyway.
        private static readonly Vector2 LabelSize = new(600f, 80f);

        public static VersionLabelView Build()
        {
            HudBuilder.RequireTextMeshProEssentials();

            var root = new GameObject("Version Label", typeof(Canvas), typeof(CanvasScaler), typeof(VersionLabelView));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = HudBuilder.ReferenceResolution;
            scaler.matchWidthOrHeight = HudBuilder.MatchHeight;

            var safeArea = new GameObject("Safe Area", typeof(RectTransform)).GetComponent<RectTransform>();
            safeArea.SetParent(root.transform, false);
            safeArea.anchorMin = Vector2.zero;
            safeArea.anchorMax = Vector2.one;
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;

            var text = new GameObject("Version", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.rectTransform.SetParent(safeArea, false);
            text.rectTransform.sizeDelta = LabelSize;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.text = string.Empty;

            var view = root.GetComponent<VersionLabelView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("canvas").objectReferenceValue = canvas;
            serialized.FindProperty("safeArea").objectReferenceValue = safeArea;
            serialized.FindProperty("label").objectReferenceValue = text;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }
    }
}
