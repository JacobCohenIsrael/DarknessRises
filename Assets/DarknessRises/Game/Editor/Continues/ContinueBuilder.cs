using DarknessRises.Game.Ads;
using DarknessRises.Game.Continues;
using DarknessRises.Game.Editor.Health;
using UnityEditor;
using UnityEngine;

namespace DarknessRises.Game.Editor.Continues
{
    /// <summary>
    /// Builds Continue: its definition asset, the burst of light on the character, and the fake ad's placeholder screen
    /// on the Screens canvas. The death screen's Continue button is built with the rest of the death screen
    /// (<see cref="HealthBuilder"/>). Call it after <see cref="HealthBuilder.Build"/>.
    /// </summary>
    public static class ContinueBuilder
    {
        private const string ContinueDefinitionPath = "Assets/DarknessRises/Configuration/ContinueDefinition.asset";

        private static readonly Color AdShadeColor = new(0.02f, 0.02f, 0.03f, 1f);
        private static readonly Color AdTitleColor = new(0.95f, 0.8f, 0.2f, 1f);
        private const float AdBodyFontSize = 40f;

        private const string AdBody = "A placeholder for a real ad. It reports the ad as watched when the countdown ends.";
        private const string ClosableAdBody = "A placeholder for a real ad. Watch it to the end for the reward; closing it early earns nothing.";
        private const string FakeAdName = "Fake Ad";
        private const string FakeAdCountdownFormat = "Back to the Run in {0}";
        private static readonly Vector2 CloseButtonPosition = new(0f, -320f);

        /// <summary>Loads the Continue definition, creating it with default tuning if it's missing.</summary>
        public static ContinueDefinition LoadOrCreateContinue()
        {
            var definition = TestRoomBuilder.LoadOrCreate<ContinueDefinition>(ContinueDefinitionPath);

            // Re-saved so fields added since it was created are written to disk with their defaults.
            EditorUtility.SetDirty(definition);
            return definition;
        }

        public static void Build(GameRoot gameRoot, Transform character)
        {
            var serializedRoot = new SerializedObject(gameRoot);
            var screens = ((Component)serializedRoot.FindProperty("deathFlow").objectReferenceValue).transform;
            serializedRoot.FindProperty("continueBurst").objectReferenceValue = BuildBurst(character);
            serializedRoot.FindProperty("continueAd").objectReferenceValue =
                BuildFakeAd(screens, FakeAdName, FakeAdCountdownFormat, canClose: false);
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();
        }

        // At the character's feet, so the burst goes off where it died. The light sits above, at its authored height.
        private static ContinueBurstView BuildBurst(Transform character)
        {
            var burst = new GameObject("Continue Burst", typeof(ContinueBurstView));
            burst.transform.SetParent(character, false);

            var light = new GameObject("Light", typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(burst.transform, false);
            light.type = LightType.Point;
            light.shadows = LightShadows.None;
            light.enabled = false;

            var view = burst.GetComponent<ContinueBurstView>();
            TestRoomBuilder.SetReference(view, "burstLight", light);
            return view;
        }

        /// <summary>
        /// A fake ad's placeholder screen on the Screens canvas, counting down with <paramref name="countdownFormat"/>
        /// ({0} is the seconds left). With <paramref name="canClose"/> it has a button that closes it early, which
        /// reports the ad as not watched.
        /// </summary>
        internal static FakeAdView BuildFakeAd(Transform screens, string name, string countdownFormat, bool canClose)
        {
            var panel = HealthBuilder.AddPanel(screens, name, AdShadeColor);
            HealthBuilder.AddText(
                panel, "Title", "ADVERTISEMENT", HealthBuilder.TitleFontSize, AdTitleColor,
                new Vector2(0f, 160f), new Vector2(1400f, 160f));
            HealthBuilder.AddText(
                panel, "Body", canClose ? ClosableAdBody : AdBody,
                AdBodyFontSize, HealthBuilder.TextColor, new Vector2(0f, 10f), new Vector2(1400f, 120f));
            var countdown = HealthBuilder.AddText(
                panel, "Countdown", string.Empty, HealthBuilder.StatFontSize, HealthBuilder.TitleColor,
                new Vector2(0f, -140f), new Vector2(1000f, 80f));

            var view = panel.gameObject.AddComponent<FakeAdView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("countdown").objectReferenceValue = countdown;
            serialized.FindProperty("countdownFormat").stringValue = countdownFormat;
            if (canClose)
            {
                serialized.FindProperty("closeButton").objectReferenceValue = HealthBuilder.AddButton(panel, "Close Ad", CloseButtonPosition);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            panel.gameObject.SetActive(false);
            return view;
        }
    }
}
