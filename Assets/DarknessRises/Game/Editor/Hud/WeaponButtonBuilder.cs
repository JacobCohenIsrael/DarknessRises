using DarknessRises.Game.Gestures;
using DarknessRises.Game.Hud;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Editor.Hud
{
    /// <summary>
    /// Builds the Weapon button: a small tile at the bottom right of the HUD, just left of the magazine count, showing
    /// the equipped Primary weapon's icon and name over its ammo reserve. A tap or click on it presses the Next Weapon command.
    /// It sits in the ammo corner, clear of the Grenade button, the Skill buttons and the pause button, and has nothing
    /// on it that looks like a movement control. Call it once the HUD, the Grenade button and the Gesture input are built.
    /// </summary>
    public static class WeaponButtonBuilder
    {
        // The magazine count is 360 wide, 56 from the right edge and 48 from the bottom: the tile sits beside it.
        private static readonly Vector2 Margin = new(432f, 48f);
        private static readonly Vector2 Size = new(250f, 120f);
        private const float Padding = 10f;
        private const float NameFontSize = 30f;
        private const float ReserveFontSize = 26f;

        // The icon fills the left of the upper half; the name takes the rest. A silhouette is twice as wide as tall.
        private const float IconWidth = 96f;
        private const float IconGap = 14f;

        // Until the Weapon button's settings colour them at runtime.
        private static readonly Color FaceColor = new(0.1f, 0.1f, 0.11f, 0.6f);
        private static readonly Color TextColor = new(0.9f, 0.9f, 0.9f, 0.95f);

        public static void Build(GameRoot gameRoot)
        {
            var serializedRoot = new SerializedObject(gameRoot);
            var hud = ((Component)serializedRoot.FindProperty("ammoHud").objectReferenceValue).transform.parent.gameObject;
            var gestures = (GestureInput)serializedRoot.FindProperty("gestureInput").objectReferenceValue;

            // The button is pressed through the canvas's raycaster, like the Grenade button.
            if (!hud.TryGetComponent<GraphicRaycaster>(out _))
            {
                hud.AddComponent<GraphicRaycaster>();
            }

            var tile = new GameObject("Weapon Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(WeaponButtonView));
            var rect = tile.GetComponent<RectTransform>();
            rect.SetParent(hud.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-Margin.x, Margin.y);
            rect.sizeDelta = Size;
            var face = tile.GetComponent<Image>();
            face.color = FaceColor;
            var button = tile.GetComponent<Button>();
            button.targetGraphic = face;

            var icon = AddIcon(rect);
            var weaponName = AddLine(rect, "Name", "PISTOL", NameFontSize, upperHalf: true);
            weaponName.fontStyle = FontStyles.Bold;
            weaponName.rectTransform.offsetMin += Vector2.right * (IconWidth + IconGap);
            var reserve = AddLine(rect, "Reserve", "RESERVE 0", ReserveFontSize, upperHalf: false);

            var view = tile.GetComponent<WeaponButtonView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.FindProperty("face").objectReferenceValue = face;
            serialized.FindProperty("icon").objectReferenceValue = icon;
            serialized.FindProperty("weaponName").objectReferenceValue = weaponName;
            serialized.FindProperty("reserve").objectReferenceValue = reserve;
            serialized.FindProperty("gestures").objectReferenceValue = gestures;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            serializedRoot.FindProperty("weaponButton").objectReferenceValue = view;
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();
        }

        // The equipped Weapon's silhouette, left of its name, kept in proportion.
        private static Image AddIcon(RectTransform tile)
        {
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            var rect = icon.rectTransform;
            rect.SetParent(tile, false);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = new Vector2(Padding, 0f);
            rect.offsetMax = new Vector2(Padding + IconWidth, -Padding);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = TextColor;
            return icon;
        }

        // One line of the tile, filling its upper or lower half, kept on one line however long the Weapon's name is.
        private static TextMeshProUGUI AddLine(RectTransform tile, string name, string text, float fontSize, bool upperHalf)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            var rect = label.rectTransform;
            rect.SetParent(tile, false);
            rect.anchorMin = new Vector2(0f, upperHalf ? 0.5f : 0f);
            rect.anchorMax = new Vector2(1f, upperHalf ? 1f : 0.5f);
            rect.offsetMin = new Vector2(Padding, upperHalf ? 0f : Padding);
            rect.offsetMax = new Vector2(-Padding, upperHalf ? -Padding : 0f);

            label.text = text;
            label.fontSize = fontSize;
            label.color = TextColor;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = fontSize * 0.55f;
            label.fontSizeMax = fontSize;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }
    }
}
