using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace DarknessRises.Game.Editor.Content
{
    /// <summary>How long a positional horror sound is, which decides how it's kept in memory on a phone.</summary>
    public enum HorrorClipLength
    {
        /// <summary>A second or two (a footstep, a squeak): decompressed when it loads, so it plays on time.</summary>
        Short,

        /// <summary>A few seconds (a hiss, a breath): kept compressed in memory and decoded as it plays.</summary>
        Medium,

        /// <summary>Tens of seconds or a big file (a whisper, an ambient bed, a scratching loop): streamed, never held whole.</summary>
        Long,
    }

    /// <summary>
    /// The import settings of the approved horror sounds (footsteps, Critters, Phantom eyes, the ambient bed), set from
    /// code so nothing is set by hand: mono, since every one is placed in 3D, where a mono clip places cleanly and stereo
    /// only doubles its memory; Vorbis; and a load type by length (<see cref="HorrorClipLength"/>), so the long ones never
    /// sit whole in a phone's memory. Also swaps a definition's placeholder clips for the approved ones.
    /// </summary>
    public static class HorrorSoundImport
    {
        /// <summary>Loads the clip at <paramref name="path"/>, reimporting it first if its settings differ.</summary>
        public static AudioClip Load(string path, HorrorClipLength length)
        {
            if (AssetImporter.GetAtPath(path) is AudioImporter importer)
            {
                var settings = importer.defaultSampleSettings;
                var loadType = length switch
                {
                    HorrorClipLength.Short => AudioClipLoadType.DecompressOnLoad,
                    HorrorClipLength.Medium => AudioClipLoadType.CompressedInMemory,
                    _ => AudioClipLoadType.Streaming,
                };

                // Long clips are ambience: Unity picks the lowest sample rate that keeps what's in them.
                var sampleRate = length == HorrorClipLength.Long ? AudioSampleRateSetting.OptimizeSampleRate : AudioSampleRateSetting.PreserveSampleRate;
                if (!importer.forceToMono
                    || settings.loadType != loadType
                    || settings.compressionFormat != AudioCompressionFormat.Vorbis
                    || settings.sampleRateSetting != sampleRate)
                {
                    importer.forceToMono = true;
                    settings.loadType = loadType;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.sampleRateSetting = sampleRate;
                    importer.defaultSampleSettings = settings;
                    importer.SaveAndReimport();
                }
            }

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"[HorrorSoundImport] No clip at {path}.");
            }

            return clip;
        }

        /// <summary>
        /// Points the clip list <paramref name="clips"/> at the approved clips at <paramref name="paths"/>, if it's empty
        /// or still holds only placeholders (clips at <paramref name="placeholderPaths"/>). A list chosen in the Inspector
        /// is kept. Every approved clip gets its import settings either way, and is marked addressable in
        /// <paramref name="groupName"/>.
        /// </summary>
        public static void ReplacePlaceholders(
            SerializedProperty clips,
            IReadOnlyList<string> placeholderPaths,
            IReadOnlyList<string> paths,
            HorrorClipLength length,
            string groupName)
        {
            var approved = new AudioClip[paths.Count];
            for (var i = 0; i < paths.Count; i++)
            {
                approved[i] = Load(paths[i], length);
            }

            if (!OnlyPlaceholders(clips, placeholderPaths))
            {
                return;
            }

            clips.arraySize = approved.Length;
            for (var i = 0; i < approved.Length; i++)
            {
                AddressableContent.Reference(clips.GetArrayElementAtIndex(i), approved[i], groupName);
            }
        }

        /// <summary>
        /// Sets the float fields at <paramref name="paths"/> of <paramref name="serialized"/> back to the values a fresh
        /// <typeparamref name="T"/> has: the tuning written for the approved clips, replacing what was tuned to make a
        /// placeholder pass (its pitch, say).
        /// </summary>
        public static void ResetToDefaults<T>(SerializedObject serialized, params string[] paths) where T : ScriptableObject
        {
            var fresh = ScriptableObject.CreateInstance<T>();
            try
            {
                var defaults = new SerializedObject(fresh);
                foreach (var path in paths)
                {
                    serialized.FindProperty(path).floatValue = defaults.FindProperty(path).floatValue;
                }
            }
            finally
            {
                Object.DestroyImmediate(fresh);
            }
        }

        /// <summary>Takes a placeholder clip that nothing uses any more out of Addressables, so no bundle carries it.</summary>
        public static void Unmark(string path)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (settings != null && !string.IsNullOrEmpty(guid) && settings.FindAssetEntry(guid) != null)
            {
                settings.RemoveAssetEntry(guid, postEvent: false);
                EditorUtility.SetDirty(settings);
            }
        }

        private static bool OnlyPlaceholders(SerializedProperty clips, IReadOnlyList<string> placeholderPaths)
        {
            for (var i = 0; i < clips.arraySize; i++)
            {
                var clip = AddressableContent.Referenced<AudioClip>(clips.GetArrayElementAtIndex(i));
                if (clip == null)
                {
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(clip);
                var isPlaceholder = false;
                for (var j = 0; j < placeholderPaths.Count; j++)
                {
                    isPlaceholder |= path == placeholderPaths[j];
                }

                if (!isPlaceholder)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
