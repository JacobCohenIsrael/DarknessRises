using DarknessRises.Game.Credits;
using DarknessRises.Game.Death;
using DarknessRises.Game.Editor.Content;
using UnityEditor;
using UnityEngine;

namespace DarknessRises.Game.Editor.Credits
{
    /// <summary>
    /// Builds Credits: their definition asset with its placeholder sound, and the end screen's place on the
    /// <see cref="GameRoot"/>. The end screen itself, with its pay-out lines, is built with the other screens
    /// (<see cref="Health.HealthBuilder"/>).
    /// Call <see cref="Build"/> once the death and end screens and the Continue are built.
    /// </summary>
    public static class CreditsBuilder
    {
        private const string CreditsDefinitionPath = "Assets/DarknessRises/Configuration/CreditsDefinition.asset";

        // An approved Free Pack placeholder, shared with the Clear banner: its coins start after a quiet lead-in.
        private const string BonusClipPath = "Assets/Free Pack/Magic Spell_Coins_2.wav";
        private const float BonusClipStartSeconds = 1.3f;

        /// <summary>Loads the Credits definition, creating it with default tuning and its placeholder sound if it's missing.</summary>
        public static CreditsDefinition LoadOrCreateCredits()
        {
            var definition = TestRoomBuilder.LoadOrCreate<CreditsDefinition>(CreditsDefinitionPath);
            var serialized = new SerializedObject(definition);
            var clip = serialized.FindProperty("endScreen.payoutClip");
            if (AddressableContent.Referenced<AudioClip>(clip) == null)
            {
                serialized.FindProperty("endScreen.payoutClipStartSeconds").floatValue = BonusClipStartSeconds;
            }

            AddressableContent.ReferenceIfEmpty(clip, () => GameplaySoundImport.Load(BonusClipPath), AddressableContent.SharedGroup);
            GameplaySoundImport.ApplyToReferenced(clip);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Re-saved so fields added since it was created are written to disk with their defaults.
            EditorUtility.SetDirty(definition);
            return definition;
        }

        public static void Build(GameRoot gameRoot)
        {
            var serializedRoot = new SerializedObject(gameRoot);
            var deathFlow = (DeathFlow)serializedRoot.FindProperty("deathFlow").objectReferenceValue;
            var endScreen = (EndScreenView)new SerializedObject(deathFlow).FindProperty("endScreen").objectReferenceValue;

            serializedRoot.FindProperty("endScreen").objectReferenceValue = endScreen;
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
