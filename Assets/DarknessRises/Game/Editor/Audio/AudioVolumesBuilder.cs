using DarknessRises.Game.Audio;
using DarknessRises.Game.Configuration;
using DarknessRises.Game.Editor.Content;
using UnityEditor;
using UnityEngine;

namespace DarknessRises.Game.Editor.Audio
{
    /// <summary>
    /// Builds what makes the volume sliders work in a scene (ADR 0010): the <see cref="SceneAudioVolumes"/> on a root
    /// object, which every categorized source of the scene follows, and authors the sliders' sample sound.
    /// </summary>
    public static class AudioVolumesBuilder
    {
        public const string RootName = "Audio Volumes";

        // An approved Free Pack placeholder: the Pistol's own Gunshot, the sound players hear most.
        private const string SampleClipPath = "Assets/Free Pack/Hand Gun 1.wav";

        /// <summary>Points the sliders' sample at its placeholder if it's empty. A sample already set stays as it was tuned.</summary>
        public static void AuthorSettings(GameConfiguration configuration)
        {
            var serialized = new SerializedObject(configuration);
            AddressableContent.ReferenceIfEmpty(
                serialized.FindProperty("audioVolumes.sampleClip"),
                () => GameplaySoundImport.Load(SampleClipPath),
                AddressableContent.SharedGroup);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Written with the fields' defaults, the first time they're there.
            EditorUtility.SetDirty(configuration);
        }

        /// <summary>The scene's volumes, on a root object of the scene being built.</summary>
        public static SceneAudioVolumes Build(GameConfiguration configuration)
        {
            var volumes = new GameObject(RootName, typeof(SceneAudioVolumes)).GetComponent<SceneAudioVolumes>();
            var serialized = new SerializedObject(volumes);
            serialized.FindProperty("configuration").objectReferenceValue = configuration;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return volumes;
        }
    }
}
