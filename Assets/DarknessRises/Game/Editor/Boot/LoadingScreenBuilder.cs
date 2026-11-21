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
    /// over the whole screen, a label in the middle, and under it a thin download bar (a track with a fill stretched from
    /// its left edge) and the sizes. It's built covering the screen with the label, the bar and the sizes off, so the app
    /// opens on black. Where the bar sits and how big it is come from the settings. There is no raycaster: it never takes a tap. Its look and timing come from the
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
        private static readonly Vector2 BarSize = new(520f, 4f);
        private static readonly Vector2 BarPosition = new(0f, -48f);
        private static readonly Vector2 SizesSize = new(600f, 48f);
        private static readonly Vector2 SizesPosition = new(0f, -80f);
        private const float SizesFontSize = 24f;

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

            var barTrack = new GameObject("Download Bar", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            var trackRect = barTrack.rectTransform;
            trackRect.SetParent(root.transform, false);
            trackRect.anchorMin = trackRect.anchorMax = trackRect.pivot = new Vector2(0.5f, 0.5f);
            trackRect.anchoredPosition = BarPosition;
            trackRect.sizeDelta = BarSize;
            barTrack.color = new Color(LabelColor.r, LabelColor.g, LabelColor.b, 0.15f);
            barTrack.raycastTarget = false;

            // Stretched from the track's left edge to as far as the download has got: the view moves its right anchor.
            var barFill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            var fillRect = barFill.rectTransform;
            fillRect.SetParent(trackRect, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            barFill.color = LabelColor;
            barFill.raycastTarget = false;
            barTrack.gameObject.SetActive(false);

            var sizes = new GameObject("Sizes", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            var sizesRect = sizes.rectTransform;
            sizesRect.SetParent(root.transform, false);
            sizesRect.anchorMin = sizesRect.anchorMax = sizesRect.pivot = new Vector2(0.5f, 0.5f);
            sizesRect.anchoredPosition = SizesPosition;
            sizesRect.sizeDelta = SizesSize;
            sizes.text = string.Empty;
            sizes.fontSize = SizesFontSize;
            sizes.color = LabelColor;
            sizes.alignment = TextAlignmentOptions.Center;
            sizes.textWrappingMode = TextWrappingModes.NoWrap;
            sizes.raycastTarget = false;
            sizes.gameObject.SetActive(false);

            var view = root.GetComponent<LoadingScreenView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("canvas").objectReferenceValue = canvas;
            serialized.FindProperty("cover").objectReferenceValue = cover;
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.FindProperty("barTrack").objectReferenceValue = barTrack;
            serialized.FindProperty("barFill").objectReferenceValue = barFill;
            serialized.FindProperty("sizes").objectReferenceValue = sizes;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }
    }
}
