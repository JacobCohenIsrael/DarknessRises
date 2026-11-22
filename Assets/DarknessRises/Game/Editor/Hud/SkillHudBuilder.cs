using DarknessRises.Game.Controls;
using DarknessRises.Game.Hud;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Editor.Hud
{
    /// <summary>
    /// Builds the Skill buttons on the HUD: a row at the bottom left of the safe area with one template button, which
    /// the <see cref="SkillBarView"/> copies for each Skill the character has. Each button is an icon on a dark disc
    /// with a rim, the Skill's name under it and a key badge in its corner; colours, icons and names come from each
    /// Skill's authored look at runtime, so nothing here is per Skill.
    /// </summary>
    public static class SkillHudBuilder
    {
        private static readonly Vector2 Margin = new(56f, 48f);
        private const float DiscSize = 120f;
        private const float RingThickness = 9f;
        private const float IconInset = 25f;
        private const float Gap = 40f;
        private const float NameHeight = 34f;
        private const float BadgeSize = 44f;
        private static readonly Vector2 BadgeOffset = new(4f, 4f);

        private const float SecondsFontSize = 48f;
        private const float NameFontSize = 24f;
        private const float KeyFontSize = 26f;

        // Placeholders until the Skills' looks and the settings colour them at runtime.
        private static readonly Color DiscColor = new(0.06f, 0.06f, 0.08f, 0.78f);
        private static readonly Color SweepColor = new(0f, 0f, 0f, 0.62f);
        private static readonly Color TextColor = new(0.92f, 0.92f, 0.92f, 0.95f);
        private static readonly Color BadgeColor = new(0.92f, 0.92f, 0.92f, 0.95f);
        private static readonly Color KeyColor = new(0.06f, 0.06f, 0.08f, 1f);

        /// <summary>Builds the row of Skill buttons on <paramref name="hud"/>, its key badges following <paramref name="controls"/>.</summary>
        public static SkillBarView BuildSkillBar(Canvas hud, PlayerControls controls)
        {
            var safeArea = HudBuilder.SafeArea(hud.transform);
            var bar = new GameObject("Skill Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(SkillBarView));
            var barRect = bar.GetComponent<RectTransform>();
            barRect.SetParent(safeArea, false);
            barRect.anchorMin = barRect.anchorMax = barRect.pivot = Vector2.zero;
            barRect.anchoredPosition = Margin;
            barRect.sizeDelta = new Vector2(0f, DiscSize + NameHeight);

            var layout = bar.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = Gap;
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var template = BuildButton(barRect, controls);
            var view = bar.GetComponent<SkillBarView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("template").objectReferenceValue = template;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static SkillHudView BuildButton(RectTransform bar, PlayerControls controls)
        {
            var disc = HudIcons.Disc();
            var ring = HudIcons.Ring();

            var root = new GameObject("Skill Button", typeof(RectTransform), typeof(SkillHudView));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(bar, false);
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(DiscSize, DiscSize + NameHeight);

            // The disc and all on it, scaled about its middle for the ready pulse.
            var faceObject = new GameObject("Face", typeof(RectTransform));
            var face = faceObject.GetComponent<RectTransform>();
            face.SetParent(rect, false);
            face.anchorMin = face.anchorMax = new Vector2(0.5f, 1f);
            face.pivot = new Vector2(0.5f, 0.5f);
            face.sizeDelta = new Vector2(DiscSize, DiscSize);
            face.anchoredPosition = new Vector2(0f, -DiscSize / 2f);

            // Drawn back to front: the active ring peeks out around the disc, the sweep darkens the icon.
            var activeRing = AddImage(face, "Active Ring", disc, Color.white, -RingThickness);
            MakeRadial(activeRing);
            var discImage = AddImage(face, "Disc", disc, DiscColor, 0f);
            var rim = AddImage(face, "Rim", ring, Color.white, 0f);
            var icon = AddImage(face, "Icon", HudIcons.Flare(), Color.white, IconInset);
            icon.preserveAspect = true;
            var sweep = AddImage(face, "Cooldown Sweep", disc, SweepColor, 0f);
            MakeRadial(sweep);
            var seconds = AddText(face, "Seconds", "15", SecondsFontSize, TextColor, TextAlignmentOptions.Center);

            // The keyboard key, in a light badge on the disc's top right corner.
            var badge = AddImage(face, "Key Badge", disc, BadgeColor, 0f);
            var badgeRect = badge.rectTransform;
            badgeRect.anchorMin = badgeRect.anchorMax = Vector2.one;
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.sizeDelta = new Vector2(BadgeSize, BadgeSize);
            badgeRect.anchoredPosition = -BadgeSize / 2f * Vector2.one + BadgeOffset;
            var key = AddText(badgeRect, "Key", "Q", KeyFontSize, KeyColor, TextAlignmentOptions.Center);

            var name = AddText(rect, "Name", "SKILL", NameFontSize, TextColor, TextAlignmentOptions.Top);
            var nameRect = name.rectTransform;
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = new Vector2(1f, 0f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.offsetMin = new Vector2(-Gap / 2f, 0f);
            nameRect.offsetMax = new Vector2(Gap / 2f, NameHeight);

            var view = root.GetComponent<SkillHudView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("controls").objectReferenceValue = controls;
            serialized.FindProperty("face").objectReferenceValue = face;
            serialized.FindProperty("disc").objectReferenceValue = discImage;
            serialized.FindProperty("rim").objectReferenceValue = rim;
            serialized.FindProperty("icon").objectReferenceValue = icon;
            serialized.FindProperty("activeRing").objectReferenceValue = activeRing;
            serialized.FindProperty("cooldownSweep").objectReferenceValue = sweep;
            serialized.FindProperty("cooldownSeconds").objectReferenceValue = seconds;
            serialized.FindProperty("nameLabel").objectReferenceValue = name;
            serialized.FindProperty("keyBadge").objectReferenceValue = badge.gameObject;
            serialized.FindProperty("keyLabel").objectReferenceValue = key;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            activeRing.gameObject.SetActive(false);
            sweep.gameObject.SetActive(false);
            seconds.gameObject.SetActive(false);
            return view;
        }

        private static Image AddImage(RectTransform parent, string name, Sprite sprite, Color color, float inset)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = -Vector2.one * inset;

            var image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void MakeRadial(Image image)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = false;
            image.fillAmount = 1f;
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
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;
            return label;
        }
    }
}
