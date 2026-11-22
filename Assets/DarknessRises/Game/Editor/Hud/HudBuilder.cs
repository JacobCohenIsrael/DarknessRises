using DarknessRises.Game.Hud;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Editor.Hud
{
    /// <summary>
    /// Builds the minimal HUD: for now, the ammo readout at the bottom right.
    /// </summary>
    public static class HudBuilder
    {
        private const string TextMeshProEssentialsPackage = "Library/PackageCache/com.unity.ugui@*/Package Resources/TMP Essential Resources.unitypackage";
        private const string TextMeshProSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        // Landscape phone reference resolution; the HUD scales with screen height.
        internal static readonly Vector2 ReferenceResolution = new(1920f, 1080f);
        internal const float MatchHeight = 1f;

        private static readonly Vector2 AmmoMargin = new(56f, 48f);
        private static readonly Vector2 AmmoSize = new(360f, 130f);
        private const float MagazineFontSize = 64f;
        private const float ReloadingFontSize = 28f;
        private static readonly Color MagazineColor = new(0.85f, 0.85f, 0.85f, 0.8f);
        private static readonly Color ReloadingColor = new(1f, 0.72f, 0.3f, 0.9f);

        /// <summary>
        /// Checks that TextMesh Pro's essential resources (default font and settings) are in the project.
        /// They're source-controlled; a package import can't finish inside a batch-mode method, so this only explains the fix.
        /// </summary>
        public static void RequireTextMeshProEssentials()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(TextMeshProSettingsPath) != null)
            {
                return;
            }

            throw new System.InvalidOperationException(
                "TextMesh Pro essential resources are missing. Import them with Window > TextMeshPro > Import TMP Essential Resources, " +
                $"or headless with: unity run . -- -importPackage \"<absolute path to {TextMeshProEssentialsPackage}>\".");
        }

        public static AmmoHudView BuildAmmoHud()
        {
            var hud = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler));
            hud.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = hud.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = MatchHeight;

            var ammo = new GameObject("Ammo", typeof(RectTransform), typeof(AmmoHudView));
            var ammoRect = ammo.GetComponent<RectTransform>();
            ammoRect.SetParent(hud.transform, false);
            ammoRect.anchorMin = ammoRect.anchorMax = ammoRect.pivot = new Vector2(1f, 0f);
            ammoRect.anchoredPosition = new Vector2(-AmmoMargin.x, AmmoMargin.y);
            ammoRect.sizeDelta = AmmoSize;

            var magazineCount = AddText(ammoRect, "Magazine Count", "12 / 12", MagazineFontSize, MagazineColor, TextAlignmentOptions.BottomRight);
            var reloading = AddText(ammoRect, "Reloading", "RELOADING", ReloadingFontSize, ReloadingColor, TextAlignmentOptions.TopRight);
            reloading.gameObject.SetActive(false);

            var view = ammo.GetComponent<AmmoHudView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("magazineCount").objectReferenceValue = magazineCount;
            serialized.FindProperty("reloadingIndicator").objectReferenceValue = reloading.gameObject;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>
        /// The HUD's safe-area frame: a child of <paramref name="hud"/> kept over the screen's safe area at runtime, for
        /// corner HUD that must stay clear of notches. Made on first use.
        /// </summary>
        public static RectTransform SafeArea(Transform hud)
        {
            const string frameName = "Safe Area";
            var existing = hud.Find(frameName);
            if (existing != null)
            {
                return (RectTransform)existing;
            }

            var frameObject = new GameObject(frameName, typeof(RectTransform), typeof(SafeAreaFrame));
            var frame = frameObject.GetComponent<RectTransform>();
            frame.SetParent(hud, false);
            frame.anchorMin = Vector2.zero;
            frame.anchorMax = Vector2.one;
            frame.offsetMin = frame.offsetMax = Vector2.zero;

            var serialized = new SerializedObject(frameObject.GetComponent<SafeAreaFrame>());
            serialized.FindProperty("frame").objectReferenceValue = frame;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return frame;
        }

        private static TextMeshProUGUI AddText(
            RectTransform parent,
            string name,
            string text,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }
    }
}
