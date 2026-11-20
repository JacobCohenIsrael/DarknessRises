using DarknessRises.Game.Ambience;
using DarknessRises.Game.Editor.Content;
using UnityEditor;
using UnityEngine;

namespace DarknessRises.Game.Editor.Ambience
{
    /// <summary>
    /// Builds the ambient bed's definition with the approved recording (bats, 24 s, streamed), marked addressable with
    /// the other creature and horror sounds and loaded with each Level. An existing asset keeps its tuning.
    /// </summary>
    public static class AmbienceBuilder
    {
        private const string ConfigurationFolder = "Assets/DarknessRises/Configuration/Ambience";
        public const string DefinitionPath = ConfigurationFolder + "/AmbientBed.asset";

        // Approved on 2026-11-20: good but long, so it's played as short slices, now and then.
        private static readonly string[] ClipPaths = { "Assets/Rain Forest SFX/Animal/Bat Short.wav" };

        public static AmbienceDefinition LoadOrCreate()
        {
            if (!AssetDatabase.IsValidFolder(ConfigurationFolder))
            {
                AssetDatabase.CreateFolder("Assets/DarknessRises/Configuration", "Ambience");
            }

            var definition = AssetDatabase.LoadAssetAtPath<AmbienceDefinition>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<AmbienceDefinition>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }

            var serialized = new SerializedObject(definition);
            HorrorSoundImport.ReplacePlaceholders(
                serialized.FindProperty("clips"),
                System.Array.Empty<string>(),
                ClipPaths,
                HorrorClipLength.Long,
                AddressableContent.EnemiesGroup);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }
    }
}
