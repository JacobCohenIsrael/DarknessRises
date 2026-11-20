using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DarknessRises.Game.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarknessRises.Game.EditModeTests.Audio
{
    /// <summary>
    /// Every sound follows a volume slider (ADR 0010): every built AudioSource has a category, every scene with sound has
    /// the volumes its sources follow, and no runtime code sets a source's volume past its category.
    /// </summary>
    public sealed class AudioRoutingTests
    {
        private const string ContentFolder = "Assets/DarknessRises";
        private const string ScenesFolder = "Assets/DarknessRises/Scenes";
        private const string RuntimeCodeFolder = "Assets/DarknessRises/Game";

        // The only runtime code that may set AudioSource.volume: it's where the category's gain is applied.
        private static readonly string[] VolumeWriters = { "AudioCategorySource.cs", "AudioSourceVolume.cs" };

        // "something.volume = ..." (not ==), which on an AudioSource would bypass its category's slider.
        private static readonly Regex VolumeWrite = new(@"\.volume\s*=(?!=)");

        [Test]
        public void Every_AudioSource_in_a_built_prefab_has_a_category()
        {
            var unrouted = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { ContentFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                AddUnrouted(prefab, path, unrouted);
            }

            Assert.That(unrouted, Is.Empty, "Route each with AudioRouting.Route in its builder.");
        }

        [Test]
        public void Every_AudioSource_in_a_built_scene_has_a_category_and_the_scene_has_volumes()
        {
            var unrouted = new List<string>();
            var withoutVolumes = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { ScenesFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var hasSound = false;
                    var hasVolumes = false;
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        hasSound |= root.GetComponentInChildren<AudioSource>(true) != null;
                        hasVolumes |= root.TryGetComponent<SceneAudioVolumes>(out _);
                        AddUnrouted(root, path, unrouted);
                    }

                    if (hasSound && !hasVolumes)
                    {
                        withoutVolumes.Add(path);
                    }
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            Assert.That(unrouted, Is.Empty, "Route each with AudioRouting.Route in its builder.");
            Assert.That(withoutVolumes, Is.Empty, "A scene with sound needs SceneAudioVolumes on a root object (AudioVolumesBuilder.Build).");
        }

        [Test]
        public void No_runtime_code_sets_a_sources_volume_past_its_category()
        {
            var writers = new List<string>();
            foreach (var file in Directory.GetFiles(RuntimeCodeFolder, "*.cs", SearchOption.AllDirectories))
            {
                var path = file.Replace('\\', '/');
                if (path.Contains("/Editor/") || path.Contains("/EditModeTests/") || System.Array.IndexOf(VolumeWriters, Path.GetFileName(path)) >= 0)
                {
                    continue;
                }

                var lines = File.ReadAllLines(file);
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].TrimStart();
                    if (!line.StartsWith("//") && VolumeWrite.IsMatch(line))
                    {
                        writers.Add($"{path}:{i + 1}: {line}");
                    }
                }
            }

            Assert.That(writers, Is.Empty, "Set a source's volume with AudioSource.SetVolume (Audio/AudioSourceVolume), so its slider applies.");
        }

        private static void AddUnrouted(GameObject root, string where, List<string> unrouted)
        {
            foreach (var source in root.GetComponentsInChildren<AudioSource>(true))
            {
                if (!source.TryGetComponent<AudioCategorySource>(out _))
                {
                    unrouted.Add($"{where}: {PathOf(source.transform)}");
                }
            }
        }

        private static string PathOf(Transform transform)
        {
            var path = transform.name;
            for (var parent = transform.parent; parent != null; parent = parent.parent)
            {
                path = $"{parent.name}/{path}";
            }

            return path;
        }
    }
}
