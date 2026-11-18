using DarknessRises.Game.Hud;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Editor.Hud
{
    /// <summary>
    /// Builds the Boss health bar on the HUD: the Boss's name over a bar with a background, a trail, a fill and two phase
    /// ticks. Only the structure is built here; sizes, colours and timing come from the configuration's
    /// <see cref="BossHealthBarSettings"/> when the bar is used. Call it once the HUD is built.
    /// </summary>
    public static class BossHealthBarBuilder
    {
        private const int PhaseTickCount = 2;

        public static void Build(GameRoot gameRoot)
        {
            var serializedRoot = new SerializedObject(gameRoot);
            var hud = ((Component)serializedRoot.FindProperty("ammoHud").objectReferenceValue).transform.parent;
            serializedRoot.FindProperty("bossHealthBar").objectReferenceValue = BuildBar(hud);
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();
        }

        private static BossHealthBarView BuildBar(Transform hud)
        {
            // Stretched over the HUD; the view places the panel inside the safe area.
            var root = new GameObject("Boss Health Bar", typeof(RectTransform), typeof(BossHealthBarView));
            var area = root.GetComponent<RectTransform>();
            area.SetParent(hud, false);
            Stretch(area);

            var panelObject = new GameObject("Panel", typeof(RectTransform));
            var panel = panelObject.GetComponent<RectTransform>();
            panel.SetParent(area, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 1f);

            var nameObject = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            var nameRect = nameObject.GetComponent<RectTransform>();
            nameRect.SetParent(panel, false);
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = Vector2.one;
            nameRect.pivot = new Vector2(0.5f, 1f);
            var bossName = nameObject.GetComponent<TextMeshProUGUI>();
            bossName.text = "Colossus";
            bossName.alignment = TextAlignmentOptions.Bottom;
            bossName.raycastTarget = false;

            var barObject = new GameObject("Bar", typeof(RectTransform));
            var bar = barObject.GetComponent<RectTransform>();
            bar.SetParent(panel, false);
            bar.anchorMin = Vector2.zero;
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);

            var background = AddImage(bar, "Background");
            Stretch(background.rectTransform);
            var trail = AddImage(bar, "Trail");
            FillFromLeft(trail.rectTransform);
            var fill = AddImage(bar, "Fill");
            FillFromLeft(fill.rectTransform);

            var ticks = new Image[PhaseTickCount];
            for (var i = 0; i < ticks.Length; i++)
            {
                ticks[i] = AddImage(bar, $"Phase {i + 2} Tick");
            }

            var view = root.GetComponent<BossHealthBarView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("panel").objectReferenceValue = panel;
            serialized.FindProperty("bossName").objectReferenceValue = bossName;
            serialized.FindProperty("bar").objectReferenceValue = bar;
            serialized.FindProperty("background").objectReferenceValue = background;
            serialized.FindProperty("trail").objectReferenceValue = trail;
            serialized.FindProperty("fill").objectReferenceValue = fill;
            var tickProperty = serialized.FindProperty("phaseTicks");
            tickProperty.arraySize = ticks.Length;
            for (var i = 0; i < ticks.Length; i++)
            {
                tickProperty.GetArrayElementAtIndex(i).objectReferenceValue = ticks[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            panelObject.SetActive(false);
            return view;
        }

        private static Image AddImage(RectTransform parent, string name)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var image = imageObject.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        // The view moves the right edge (anchorMax.x) to the share of the bar it fills.
        private static void FillFromLeft(RectTransform rect)
        {
            Stretch(rect);
            rect.pivot = new Vector2(0f, 0.5f);
        }
    }
}
