using DarknessRises.Game.Audio;
using DarknessRises.Game.Editor.Audio;
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

        // The Free Pack placeholder of the first build, replaced by the clips approved on 2026-11-20: a whisper, a
        // breath and a hush, played faintly from a random point. The whisper and breath are long: they stream.
        public static readonly string[] WhisperPlaceholderPaths = { "Assets/Free Pack/Ghost Manifestation 1.wav" };
        private static readonly string[] WhisperClipPaths =
        {
            "Assets/Horror Elements/Misc/Misc_Whisper.wav",
            "Assets/Horror Elements/Misc/Misc_breath.wav",
        };

        private static readonly string[] HushClipPaths = { "Assets/Horror Elements/Misc/Misc_Shh.wav" };

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

            // The Phantom eyes' whisper and breath are Ambient. A prefab from before the categories gets it here.
            AudioRouting.RouteUnroutedInPrefab(PrefabPath, AudioCategory.Ambient);
            var clips = serialized.FindProperty("whisperClips");
            var swapping = clips.arraySize == 0 || IsOnlyPlaceholder(clips);
            HorrorSoundImport.ReplacePlaceholders(clips, WhisperPlaceholderPaths, WhisperClipPaths, HorrorClipLength.Long, AddressableContent.EnemiesGroup);
            var hush = HorrorSoundImport.Load(HushClipPaths[0], HorrorClipLength.Medium);
            if (swapping)
            {
                HorrorSoundImport.ResetToDefaults<PhantomEyesDefinition>(serialized, "whisperMinPitch", "whisperMaxPitch");
            }

            if (swapping && hush != null)
            {
                clips.arraySize++;
                AddressableContent.Reference(clips.GetArrayElementAtIndex(clips.arraySize - 1), hush, AddressableContent.EnemiesGroup);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);

            // Nothing plays the placeholder any more: no bundle should carry it.
            HorrorSoundImport.Unmark(WhisperPlaceholderPaths[0]);
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
            AudioRouting.Route(voice, AudioCategory.Ambient);

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

        private static bool IsOnlyPlaceholder(SerializedProperty clips)
        {
            for (var i = 0; i < clips.arraySize; i++)
            {
                var clip = AddressableContent.Referenced<AudioClip>(clips.GetArrayElementAtIndex(i));
                if (clip != null && AssetDatabase.GetAssetPath(clip) != WhisperPlaceholderPaths[0])
                {
                    return false;
                }
            }

            return true;
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
