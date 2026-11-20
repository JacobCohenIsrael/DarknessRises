using DarknessRises.Game.Critters;
using DarknessRises.Game.Editor.Content;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DarknessRises.Game.Editor.Critters
{
    /// <summary>
    /// Builds the Critter: its definition asset and a placeholder rat prefab (a low dark body, a head, a tail and two
    /// small glinting eyes). Its prefab and sounds are Enemies-group content, loaded with each Level. Existing assets are
    /// kept, so tuning done in the Inspector survives a rebuild.
    /// </summary>
    public static class CritterBuilder
    {
        private const string Root = "Assets/DarknessRises";
        private const string ConfigurationFolder = Root + "/Configuration/Critters";
        private const string PrefabsFolder = Root + "/Prefabs/Critters";
        private const string MaterialsFolder = Root + "/Materials";
        public const string RatDefinitionPath = ConfigurationFolder + "/Rat.asset";
        private const string RatPrefabPath = PrefabsFolder + "/Rat.prefab";
        private const string BodyMaterialPath = MaterialsFolder + "/CritterBody.mat";

        // The Enemies' glossy eye material: dark until its emission is driven at runtime.
        private const string EyeMaterialPath = MaterialsFolder + "/ShamblerEyeGlint.mat";
        private const string LitShaderName = "Universal Render Pipeline/Lit";

        // The Free Pack placeholders of the first build, replaced by the clips approved on 2026-11-20.
        public static readonly string[] ScurryPlaceholderPaths = { "Assets/Free Pack/Bullet Impacts - Multiple 1.wav" };
        public static readonly string[] SqueakPlaceholderPaths = { "Assets/Free Pack/Laser Impact Light_6.wav" };

        // Short and medium clips, kept in memory; the long scratching recording streams (15 MB on disk).
        private static readonly string[] ScurryClipPaths =
        {
            "Assets/Game Sound Solutions - 8 bits Elements/OTHER SOUND PACK DEMO/Fantasy Creatures Demo/rats_1.wav",
            "Assets/Animals!/Wavs/Sneek/sneek_hiss_01.wav",
        };

        private const string ScratchClipPath = "Assets/music/darkness/dk-scratch.aif";
        private static readonly string[] SqueakClipPaths = { "Assets/Animals!/Ogg/Owl/owl_squeak_01.ogg" };

        // Placeholder rat, in metres and degrees: about 40 cm nose to tail, low to the floor, nearly black.
        private static readonly Color BodyColor = new(0.07f, 0.06f, 0.055f);
        private const float BodySmoothness = 0.25f;
        private const float HitboxRadius = 0.14f;
        private const float HitboxLength = 0.5f;
        private const float HitboxHeight = 0.1f;
        private const float EyeSize = 0.055f;
        private static readonly string[] EyeNames = { "Left Eye", "Right Eye" };
        private static readonly Vector3[] EyePositions = { new(-0.045f, 0.12f, 0.27f), new(0.045f, 0.12f, 0.27f) };

        private static readonly Part[] Parts =
        {
            new("Body", PrimitiveType.Sphere, new Vector3(0f, 0.08f, 0f), Vector3.zero, new Vector3(0.2f, 0.13f, 0.36f)),
            new("Head", PrimitiveType.Sphere, new Vector3(0f, 0.09f, 0.21f), Vector3.zero, new Vector3(0.12f, 0.1f, 0.16f)),
            new("Tail", PrimitiveType.Capsule, new Vector3(0f, 0.04f, -0.32f), new Vector3(90f, 0f, 0f), new Vector3(0.025f, 0.16f, 0.025f)),
        };

        /// <summary>Loads the rat's definition, creating it, its prefab and its sound references if they're missing.</summary>
        public static CritterDefinition LoadOrCreateRat()
        {
            EnsureFolder(ConfigurationFolder);
            var rat = AssetDatabase.LoadAssetAtPath<CritterDefinition>(RatDefinitionPath);
            if (rat == null)
            {
                rat = ScriptableObject.CreateInstance<CritterDefinition>();
                AssetDatabase.CreateAsset(rat, RatDefinitionPath);
            }

            var serialized = new SerializedObject(rat);
            AddressableContent.ReferenceIfEmpty(
                serialized.FindProperty("prefab"),
                () => LoadOrCreatePrefab().gameObject,
                AddressableContent.EnemiesGroup);
            var scurries = serialized.FindProperty("sounds.scurryClips");
            var swapping = OnlyPlaceholders(scurries, ScurryPlaceholderPaths);
            HorrorSoundImport.ReplacePlaceholders(scurries, ScurryPlaceholderPaths, ScurryClipPaths, HorrorClipLength.Medium, AddressableContent.EnemiesGroup);
            if (swapping)
            {
                AddScratch(scurries);
                HorrorSoundImport.ResetToDefaults<CritterDefinition>(
                    serialized,
                    "sounds.scurryLatestStartSeconds",
                    "sounds.scurryMinPitch",
                    "sounds.scurryMaxPitch",
                    "sounds.scurryVolume",
                    "sounds.squeakMinPitch",
                    "sounds.squeakMaxPitch");
            }

            HorrorSoundImport.ReplacePlaceholders(
                serialized.FindProperty("sounds.squeakClips"),
                SqueakPlaceholderPaths,
                SqueakClipPaths,
                HorrorClipLength.Short,
                AddressableContent.EnemiesGroup);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rat);

            // Nothing plays the placeholders any more: no bundle should carry them.
            HorrorSoundImport.Unmark(ScurryPlaceholderPaths[0]);
            HorrorSoundImport.Unmark(SqueakPlaceholderPaths[0]);
            return rat;
        }

        private static bool OnlyPlaceholders(SerializedProperty clips, string[] placeholderPaths)
        {
            for (var i = 0; i < clips.arraySize; i++)
            {
                var clip = AddressableContent.Referenced<AudioClip>(clips.GetArrayElementAtIndex(i));
                if (clip != null && System.Array.IndexOf(placeholderPaths, AssetDatabase.GetAssetPath(clip)) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        // The long scratching recording joins the scurries, streamed, if the list doesn't hold it yet.
        private static void AddScratch(SerializedProperty scurries)
        {
            var scratch = HorrorSoundImport.Load(ScratchClipPath, HorrorClipLength.Long);
            if (scratch == null)
            {
                return;
            }

            for (var i = 0; i < scurries.arraySize; i++)
            {
                if (AddressableContent.Referenced<AudioClip>(scurries.GetArrayElementAtIndex(i)) == scratch)
                {
                    return;
                }
            }

            scurries.arraySize++;
            AddressableContent.Reference(scurries.GetArrayElementAtIndex(scurries.arraySize - 1), scratch, AddressableContent.EnemiesGroup);
        }

        private static CritterView LoadOrCreatePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<CritterView>(RatPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(PrefabsFolder);
            var bodyMaterial = LoadOrCreateBodyMaterial();
            var eyeMaterial = AssetDatabase.LoadAssetAtPath<Material>(EyeMaterialPath);
            var root = new GameObject("Rat");

            // Solid, not a trigger: Shots only stop at solid colliders.
            var hitbox = root.AddComponent<CapsuleCollider>();
            hitbox.direction = 2;
            hitbox.radius = HitboxRadius;
            hitbox.height = HitboxLength;
            hitbox.center = Vector3.up * HitboxHeight;

            var voice = root.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 1f;

            var renderers = new Renderer[Parts.Length + EyeNames.Length];
            for (var i = 0; i < Parts.Length; i++)
            {
                renderers[i] = AddPart(Parts[i], root.transform, bodyMaterial);
            }

            var eyes = new Renderer[EyeNames.Length];
            for (var i = 0; i < EyeNames.Length; i++)
            {
                var eye = AddPart(new Part(EyeNames[i], PrimitiveType.Sphere, EyePositions[i], Vector3.zero, Vector3.one * EyeSize), root.transform, eyeMaterial);
                eye.shadowCastingMode = ShadowCastingMode.Off;
                eye.receiveShadows = false;
                eyes[i] = eye;
                renderers[Parts.Length + i] = eye;
            }

            var view = root.AddComponent<CritterView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("body").objectReferenceValue = hitbox;
            serialized.FindProperty("voice").objectReferenceValue = voice;
            SetArray(serialized.FindProperty("renderers"), renderers);
            SetArray(serialized.FindProperty("eyes"), eyes);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, RatPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<CritterView>();
        }

        private static void SetArray(SerializedProperty list, Renderer[] renderers)
        {
            list.arraySize = renderers.Length;
            for (var i = 0; i < renderers.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            }
        }

        private static MeshRenderer AddPart(Part part, Transform parent, Material material)
        {
            var shape = GameObject.CreatePrimitive(part.Shape);
            shape.name = part.Name;
            Object.DestroyImmediate(shape.GetComponent<Collider>());
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = part.Position;
            shape.transform.localEulerAngles = part.Rotation;
            shape.transform.localScale = part.Scale;
            var renderer = shape.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static Material LoadOrCreateBodyMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(BodyMaterialPath);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find(LitShaderName)) { name = "CritterBody" };
            material.SetColor("_BaseColor", BodyColor);
            material.SetFloat("_Smoothness", BodySmoothness);
            AssetDatabase.CreateAsset(material, BodyMaterialPath);
            return material;
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

        private readonly struct Part
        {
            public Part(string name, PrimitiveType shape, Vector3 position, Vector3 rotation, Vector3 scale)
            {
                Name = name;
                Shape = shape;
                Position = position;
                Rotation = rotation;
                Scale = scale;
            }

            public string Name { get; }
            public PrimitiveType Shape { get; }
            public Vector3 Position { get; }
            public Vector3 Rotation { get; }
            public Vector3 Scale { get; }
        }
    }
}
