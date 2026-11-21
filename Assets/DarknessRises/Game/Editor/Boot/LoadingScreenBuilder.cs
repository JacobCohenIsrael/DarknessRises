using DarknessRises.Game.Boot;
using DarknessRises.Game.Editor.Hud;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Editor.Boot
{
    /// <summary>
    /// Builds the loading screen in the boot scene: its own overlay canvas, scaled like the HUD, with a cover stretched
    /// over the whole screen and a label in the middle. It's built covering the screen with the label off, so the app
    /// opens on black. There is no raycaster: it never takes a tap. Its look and timing come from the
    /// <see cref="LoadingScreenSettings"/> at runtime.
    /// </summary>
    public static class LoadingScreenBuilder
    {
        // Wide enough for the label and its dots at a heading's size; the text doesn't wrap anyway.
        private static readonly Vector2 LabelSize = new(900f, 120f);

        // Until the settings set them at runtime.
        private const float LabelFontSize = 48f;
        private const int SortingOrder = 90;
        private static readonly Color LabelColor = new(0.8f, 0.8f, 0.8f, 0.85f);

        public static LoadingScreenView Build()
        {
            HudBuilder.RequireTextMeshProEssentials();

            var root = new GameObject("Loading Screen", typeof(Canvas), typeof(CanvasScaler), typeof(LoadingScreenView));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = HudBuilder.ReferenceResolution;
            scaler.matchWidthOrHeight = HudBuilder.MatchHeight;

            var cover = new GameObject("Cover", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            var coverRect = cover.rectTransform;
            coverRect.SetParent(root.transform, false);
            coverRect.anchorMin = Vector2.zero;
            coverRect.anchorMax = Vector2.one;
            coverRect.offsetMin = coverRect.offsetMax = Vector2.zero;
            cover.color = Color.black;
            cover.raycastTarget = false;

            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            var labelRect = label.rectTransform;
            labelRect.SetParent(root.transform, false);
            labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = LabelSize;
            label.text = "LOADING";
            label.fontSize = LabelFontSize;
            label.color = LabelColor;

            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            label.gameObject.SetActive(false);

            var view = root.GetComponent<LoadingScreenView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("canvas").objectReferenceValue = canvas;
            serialized.FindProperty("cover").objectReferenceValue = cover;
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }
    }
}
