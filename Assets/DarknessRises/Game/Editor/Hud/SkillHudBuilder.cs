using DarknessRises.Game.Hud;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Editor.Hud
{
    /// <summary>
    /// Builds the Skill cooldowns on the HUD: a disc per Skill at the bottom left, Flare then Slow-mo,
    /// where touch buttons for them can go later.
    /// </summary>
    public static class SkillHudBuilder
    {
        private const string DiscSpritePath = "UI/Skin/Knob.psd";

        private static readonly Vector2 Margin = new(56f, 48f);
        private const float DiscSize = 120f;
        private const float RingThickness = 10f;
        private const float Gap = 40f;
        private const float NameHeight = 34f;

        private const float KeyFontSize = 52f;
        private const float SecondsFontSize = 48f;
        private const float NameFontSize = 24f;

        private static readonly Color FlareColor = new(1f, 0.62f, 0.35f, 0.9f);
        private static readonly Color SlowMoColor = new(0.45f, 0.7f, 1f, 0.9f);
        private const float UnavailableAlpha = 0.3f;
        private static readonly Color SweepColor = new(0f, 0f, 0f, 0.6f);
        private static readonly Color TextColor = new(0.92f, 0.92f, 0.92f, 0.95f);
        private static readonly Color KeyColor = new(0.08f, 0.08f, 0.1f, 0.9f);

        public static void BuildSkillHuds(Canvas hud, out SkillHudView flare, out SkillHudView slowMo)
        {
            flare = BuildSkillHud(hud.transform, "Flare Cooldown", "FLARE", "Q", FlareColor, 0);
            slowMo = BuildSkillHud(hud.transform, "Slow-mo Cooldown", "SLOW-MO", "E", SlowMoColor, 1);
        }

        private static SkillHudView BuildSkillHud(Transform hud, string objectName, string skillName, string key, Color color, int slot)
        {
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(DiscSpritePath);

            var root = new GameObject(objectName, typeof(RectTransform), typeof(SkillHudView));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(hud, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(Margin.x + slot * (DiscSize + Gap), Margin.y + NameHeight);
            rect.sizeDelta = new Vector2(DiscSize, DiscSize);

            // Drawn back to front: the ring peeks out around the disc, the sweep darkens it.
            var ring = AddImage(rect, "Active Ring", sprite, color, -RingThickness);
            MakeRadial(ring);
            var disc = AddImage(rect, "Disc", sprite, color, 0f);
            var sweep = AddImage(rect, "Cooldown Sweep", sprite, SweepColor, 0f);
            MakeRadial(sweep);

            var keyLabel = AddText(rect, "Key", key, KeyFontSize, KeyColor, TextAlignmentOptions.Center);
            var seconds = AddText(rect, "Seconds", "15", SecondsFontSize, TextColor, TextAlignmentOptions.Center);
            var name = AddText(rect, "Name", skillName, NameFontSize, TextColor, TextAlignmentOptions.Top);
            var nameRect = name.rectTransform;
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = new Vector2(1f, 0f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.sizeDelta = new Vector2(Gap, NameHeight);
            nameRect.anchoredPosition = Vector2.zero;

            var unavailable = color;
            unavailable.a = UnavailableAlpha;

            var view = root.GetComponent<SkillHudView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("disc").objectReferenceValue = disc;
            serialized.FindProperty("activeRing").objectReferenceValue = ring;
            serialized.FindProperty("cooldownSweep").objectReferenceValue = sweep;
            serialized.FindProperty("keyLabel").objectReferenceValue = keyLabel;
            serialized.FindProperty("cooldownSeconds").objectReferenceValue = seconds;
            serialized.FindProperty("readyColor").colorValue = color;
            serialized.FindProperty("unavailableColor").colorValue = unavailable;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            ring.gameObject.SetActive(false);
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
