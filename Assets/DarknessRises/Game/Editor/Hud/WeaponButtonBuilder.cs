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
        // The magazine count is 360 wide, 56 from the right edge and 48 from the bottom: the tile sits beside it, inside
        // the safe area. 416 wide fits the longest name, FLAMETHROWER (about 280 at the full 30), beside the icon, so every
        // name shows at the same size. At 4:3 (a 1440-wide HUD) its left edge is at 592, clear of the Skill bar's 496.
        private static readonly Vector2 Margin = new(432f, 48f);
        private static readonly Vector2 Size = new(416f, 120f);
        private const float Padding = 14f;
        private const float NameFontSize = 30f;
        private const float ReserveFontSize = 26f;

        // The icon sits in a fixed box at the left of the upper half, fitted in proportion (a silhouette is twice as wide
        // as tall); the name starts a fixed gap after it.
        private static readonly Vector2 IconBox = new(84f, 42f);
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
            rect.SetParent(HudBuilder.SafeArea(hud.transform), false);
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
            weaponName.rectTransform.offsetMin += Vector2.right * (IconBox.x + IconGap);
            weaponName.alignment = TextAlignmentOptions.MidlineLeft;
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
            // Centred in the upper half, as the name is.
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.75f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = IconBox;
            rect.anchoredPosition = new Vector2(Padding, -Padding / 2f);
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
            // The tile fits every Weapon's name at full size; shrinking is only a safety net.
            label.fontSizeMin = fontSize * 0.8f;
            label.fontSizeMax = fontSize;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }
    }
}
