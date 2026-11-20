using DarknessRises.Game.Credits;
using DarknessRises.Game.Death;
using DarknessRises.Game.Editor.Content;
using DarknessRises.Game.Editor.Continues;
using DarknessRises.Game.Enemies;
using UnityEditor;
using UnityEngine;

namespace DarknessRises.Game.Editor.Credits
{
    /// <summary>
    /// Builds Credits: their definition asset with its placeholder sound, what a Runner's kill pays, and the fake
    /// end-of-Run ad's placeholder screen on the Screens canvas. The end screen itself, with its Credits lines and the
    /// ad's offer, is built with the other screens (<see cref="Health.HealthBuilder"/>).
    /// Call <see cref="Build"/> once the death and end screens and the Continue are built.
    /// </summary>
    public static class CreditsBuilder
    {
        private const string CreditsDefinitionPath = "Assets/DarknessRises/Configuration/CreditsDefinition.asset";

        // An approved Free Pack placeholder, shared with the Clear banner: its coins start after a quiet lead-in.
        private const string BonusClipPath = "Assets/Free Pack/Magic Spell_Coins_2.wav";
        private const float BonusClipStartSeconds = 1.3f;

        // A Runner is harder to hit than a Shambler, so its kill pays more. Set once, while it still pays the default.
        private const int DefaultKillCredits = 5;
        private const int RunnerKillCredits = 8;

        private const string FakeAdName = "End Of Run Fake Ad";
        private const string FakeAdCountdownFormat = "Your Credits in {0}";

        /// <summary>Loads the Credits definition, creating it with default tuning and its placeholder sound if it's missing.</summary>
        public static CreditsDefinition LoadOrCreateCredits()
        {
            var definition = TestRoomBuilder.LoadOrCreate<CreditsDefinition>(CreditsDefinitionPath);
            var serialized = new SerializedObject(definition);
            var clip = serialized.FindProperty("endScreen.bonusClip");
            if (AddressableContent.Referenced<AudioClip>(clip) == null)
            {
                serialized.FindProperty("endScreen.bonusClipStartSeconds").floatValue = BonusClipStartSeconds;
            }

            AddressableContent.ReferenceIfEmpty(clip, () => GameplaySoundImport.Load(BonusClipPath), AddressableContent.SharedGroup);
            GameplaySoundImport.ApplyToReferenced(clip);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Re-saved so fields added since it was created are written to disk with their defaults.
            EditorUtility.SetDirty(definition);
            return definition;
        }

        /// <summary>Gives the Runner its own Credits, unless they were already tuned away from the default.</summary>
        public static void AuthorRunnerCredits(EnemyDefinition runner)
        {
            var serialized = new SerializedObject(runner);
            var credits = serialized.FindProperty("credits");
            if (credits.intValue == DefaultKillCredits)
            {
                credits.intValue = RunnerKillCredits;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public static void Build(GameRoot gameRoot)
        {
            var serializedRoot = new SerializedObject(gameRoot);
            var deathFlow = (DeathFlow)serializedRoot.FindProperty("deathFlow").objectReferenceValue;
            var endScreen = (EndScreenView)new SerializedObject(deathFlow).FindProperty("endScreen").objectReferenceValue;

            // Last on the Screens canvas, so the ad covers the end screen that offered it.
            serializedRoot.FindProperty("endScreen").objectReferenceValue = endScreen;
            serializedRoot.FindProperty("endOfRunAd").objectReferenceValue =
                ContinueBuilder.BuildFakeAd(deathFlow.transform, FakeAdName, FakeAdCountdownFormat, canClose: true);
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
