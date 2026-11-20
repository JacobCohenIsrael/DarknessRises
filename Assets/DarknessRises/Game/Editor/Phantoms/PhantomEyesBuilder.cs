using DarknessRises.Game.Editor.Content;
using DarknessRises.Game.Phantoms;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DarknessRises.Game.Editor.Phantoms
{
    /// <summary>
    /// Builds the Phantom eyes: their definition asset and a prefab of two eyes at a Shambler's height and spacing, on
    /// the Enemies' glint material, with a voice for the whisper. Like a real Enemy's eyes, so the player can't tell them
    /// apart by look; the rules (where and when they show) are what keep them fair. Enemies-group content, loaded with each
    /// Level. Existing assets are kept, so tuning done in the Inspector survives a rebuild.
    /// </summary>
    public static class PhantomEyesBuilder
    {
        private const string Root = "Assets/DarknessRises";
        private const string ConfigurationFolder = Root + "/Configuration/Phantoms";
        private const string PrefabsFolder = Root + "/Prefabs/Phantoms";
        public const string DefinitionPath = ConfigurationFolder + "/PhantomEyes.asset";
        private const string PrefabPath = PrefabsFolder + "/PhantomEyes.prefab";
        private const string EyeMaterialPath = Root + "/Materials/ShamblerEyeGlint.mat";

        // An approved Free Pack placeholder: a ghostly breath, played faintly from a random point in its first seconds.
        private static readonly string[] WhisperClipPaths = { "Assets/Free Pack/Ghost Manifestation 1.wav" };

        // A Shambler's eyes, in metres: the same height, spacing and size.
        private const float EyeSize = 0.11f;
        private static readonly string[] EyeNames = { "Left Eye", "Right Eye" };
        private static readonly Vector3[] EyePositions = { new(-0.1f, 1.64f, 0f), new(0.1f, 1.64f, 0f) };

        /// <summary>Loads the Phantom eyes' definition, creating it, its prefab and its whisper reference if they're missing.</summary>
        public static PhantomEyesDefinition LoadOrCreate()
        {
            EnsureFolder(ConfigurationFolder);
            var definition = AssetDatabase.LoadAssetAtPath<PhantomEyesDefinition>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<PhantomEyesDefinition>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }

            var serialized = new SerializedObject(definition);
            AddressableContent.ReferenceIfEmpty(
                serialized.FindProperty("prefab"),
                () => LoadOrCreatePrefab().gameObject,
                AddressableContent.EnemiesGroup);
            var clips = serialized.FindProperty("whisperClips");
            if (clips.arraySize == 0)
            {
                clips.arraySize = WhisperClipPaths.Length;
            }

            for (var i = 0; i < clips.arraySize; i++)
            {
                var path = i < WhisperClipPaths.Length ? WhisperClipPaths[i] : null;
                AddressableContent.ReferenceIfEmpty(
                    clips.GetArrayElementAtIndex(i),
                    () => path != null ? LoadMonoClip(path) : null,
                    AddressableContent.EnemiesGroup);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static PhantomEyesView LoadOrCreatePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PhantomEyesView>(PrefabPath);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(PrefabsFolder);
            var eyeMaterial = AssetDatabase.LoadAssetAtPath<Material>(EyeMaterialPath);
            var root = new GameObject("Phantom Eyes");
            var voice = root.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 1f;

            var eyes = new Renderer[EyeNames.Length];
            for (var i = 0; i < EyeNames.Length; i++)
            {
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = EyeNames[i];

                // Nothing to hit: a Shot at Phantom eyes passes through, like any miss.
                Object.DestroyImmediate(eye.GetComponent<Collider>());
                eye.transform.SetParent(root.transform, false);
                eye.transform.localPosition = EyePositions[i];
                eye.transform.localScale = Vector3.one * EyeSize;
                var renderer = eye.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = eyeMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                eyes[i] = renderer;
            }

            var view = root.AddComponent<PhantomEyesView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("voice").objectReferenceValue = voice;
            var list = serialized.FindProperty("eyes");
            list.arraySize = eyes.Length;
            for (var i = 0; i < eyes.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = eyes[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<PhantomEyesView>();
        }

        // A positional sound: a mono clip places cleanly in 3D.
        private static AudioClip LoadMonoClip(string path)
        {
            if (AssetImporter.GetAtPath(path) is AudioImporter importer && !importer.forceToMono)
            {
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
