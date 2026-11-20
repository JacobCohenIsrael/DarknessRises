using System.Collections.Generic;
using System.Text;
using DarknessRises.Game.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarknessRises.Game.Editor.Audio
{
    /// <summary>
    /// Puts every AudioSource the builders make in its <see cref="AudioCategory"/> (ADR 0010), and checks that none was
    /// missed. A builder calls <see cref="Route"/> once for each source it makes, right where it makes it; the scene and
    /// prefab builders end with <see cref="RequireRouted(Scene)"/> or <see cref="RequireRouted(GameObject, string)"/>,
    /// which fail the build on a source without a category. The EditMode suite checks the built scenes and prefabs too.
    /// </summary>
    public static class AudioRouting
    {
        private const string CategoryProperty = "category";

        /// <summary>Puts <paramref name="source"/> in <paramref name="category"/>: the one call per built source.</summary>
        public static AudioCategorySource Route(AudioSource source, AudioCategory category)
        {
            if (!source.TryGetComponent<AudioCategorySource>(out var categorized))
            {
                categorized = source.gameObject.AddComponent<AudioCategorySource>();
            }

            var serialized = new SerializedObject(categorized);
            serialized.FindProperty(CategoryProperty).enumValueIndex = (int)category;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return categorized;
        }

        /// <summary>
        /// Puts every source of the prefab at <paramref name="prefabPath"/> that has no category yet in
        /// <paramref name="category"/>, and saves it if anything changed. Brings prefabs built before the categories up
        /// to date in place; sources that have one keep it.
        /// </summary>
        public static void RouteUnroutedInPrefab(string prefabPath, AudioCategory category)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null || Unrouted(prefab).Count == 0)
            {
                return;
            }

            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                foreach (var source in Unrouted(contents))
                {
                    Route(source, category);
                }

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            RequireRouted(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), prefabPath);
        }

        /// <summary>The sources under <paramref name="root"/> (inactive ones too) that have no category.</summary>
        public static List<AudioSource> Unrouted(GameObject root)
        {
            var unrouted = new List<AudioSource>();
            foreach (var source in root.GetComponentsInChildren<AudioSource>(true))
            {
                if (!source.TryGetComponent<AudioCategorySource>(out _))
                {
                    unrouted.Add(source);
                }
            }

            return unrouted;
        }

        /// <summary>Fails if any source in the prefab <paramref name="prefab"/> has no category.</summary>
        public static void RequireRouted(GameObject prefab, string prefabPath)
        {
            var unrouted = Unrouted(prefab);
            if (unrouted.Count > 0)
            {
                throw new System.InvalidOperationException(Describe(prefabPath, unrouted));
            }
        }

        /// <summary>
        /// Fails if any source in <paramref name="scene"/> has no category, or if the scene has sound but no
        /// <see cref="SceneAudioVolumes"/> on a root object for its sources to follow.
        /// </summary>
        public static void RequireRouted(Scene scene)
        {
            var unrouted = new List<AudioSource>();
            var hasSound = false;
            var hasVolumes = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                hasSound |= root.GetComponentInChildren<AudioSource>(true) != null;
                hasVolumes |= root.TryGetComponent<SceneAudioVolumes>(out _);
                unrouted.AddRange(Unrouted(root));
            }

            if (unrouted.Count > 0)
            {
                throw new System.InvalidOperationException(Describe(scene.path, unrouted));
            }

            if (hasSound && !hasVolumes)
            {
                throw new System.InvalidOperationException(
                    $"{scene.path} has sound but no {nameof(SceneAudioVolumes)} on a root object: its volume sliders would do nothing.");
            }
        }

        private static string Describe(string where, List<AudioSource> unrouted)
        {
            var text = new StringBuilder($"{where} has AudioSources without a category (route each with {nameof(AudioRouting)}.{nameof(Route)}):");
            foreach (var source in unrouted)
            {
                text.Append("\n  ").Append(PathOf(source.transform));
            }

            return text.ToString();
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
