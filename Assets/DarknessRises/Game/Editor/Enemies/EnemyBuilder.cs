using DarknessRises.Game.Enemies;
using DarknessRises.Game.Enemies.Tells;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace DarknessRises.Game.Editor.Enemies
{
    /// <summary>
    /// Builds the Shambler: its Enemy definition asset and a placeholder prefab, a hunched figure with reaching arms
    /// and eyes that glint as a Tell. Existing assets are kept, so tuning done in the Inspector survives a rebuild.
    /// </summary>
    public static class EnemyBuilder
    {
        private const string Root = "Assets/DarknessRises";
        private const string EnemiesConfigurationFolder = Root + "/Configuration/Enemies";
        private const string EnemiesPrefabsFolder = Root + "/Prefabs/Enemies";
        private const string MaterialsFolder = Root + "/Materials";

        public const string ShamblerDefinitionPath = EnemiesConfigurationFolder + "/Shambler.asset";
        private const string ShamblerPrefabPath = EnemiesPrefabsFolder + "/Shambler.prefab";

        // Approved Free Pack placeholders.
        private const string HurtClipPath = "Assets/Free Pack/Bullet Impact 14.wav";
        private const string DeathClipPath = "Assets/Free Pack/Bloody punch.wav";
        private const string WindUpClipPath = "Assets/Free Pack/Magic Spell_Short Reverse_1.wav";
        private const string AttackClipPath = "Assets/Free Pack/Magic Spell_Simple Swoosh_6.wav";
        private static readonly string[] TellClipPaths = { "Assets/Free Pack/Monster Bite.wav" };

        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const string UnlitShaderName = "Universal Render Pipeline/Unlit";

        // Placeholder Shambler shape, in metres.
        private const float Height = 1.8f;
        private const float Radius = 0.4f;
        private static readonly Color BodyColor = new(0.2f, 0.24f, 0.19f);
        private const float EyeSize = 0.11f;
        private static readonly Color EyeColor = new(0.05f, 0.03f, 0.02f);
        private const float EyeSmoothness = 0.9f;
        private static readonly Color WindUpEyeColor = new(8f, 0.4f, 0.2f);
        private static readonly string[] EyeNames = { "Left Eye", "Right Eye" };
        private const float TellVoiceHeight = 1.6f;
        private const string EyeGlintMaterialName = "ShamblerEyeGlint";

        /// <summary>Loads the Shambler definition, creating it and its prefab if they're missing.</summary>
        public static EnemyDefinition LoadOrCreateShambler()
        {
            EnsureFolder(EnemiesConfigurationFolder);
            var shambler = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(ShamblerDefinitionPath);
            if (shambler == null)
            {
                // The definition's defaults are the Shambler: slow and tough, slower than the character walks.
                shambler = ScriptableObject.CreateInstance<EnemyDefinition>();
                AssetDatabase.CreateAsset(shambler, ShamblerDefinitionPath);

                var serializedNew = new SerializedObject(shambler);
                serializedNew.FindProperty("audio.hurtClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(HurtClipPath);
                serializedNew.FindProperty("audio.deathClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(DeathClipPath);
                serializedNew.ApplyModifiedPropertiesWithoutUndo();
            }

            var serialized = new SerializedObject(shambler);
            var prefab = serialized.FindProperty("prefab");
            if (prefab.objectReferenceValue == null)
            {
                prefab.objectReferenceValue = LoadOrCreateShamblerPrefab();
            }

            SetClipIfEmpty(serialized.FindProperty("audio.windUpClip"), WindUpClipPath);
            SetClipIfEmpty(serialized.FindProperty("audio.attackClip"), AttackClipPath);

            // Tell sounds were added after the Shambler; give an older asset the placeholders once.
            var tellClips = serialized.FindProperty("tells.sounds.clips");
            if (tellClips.arraySize == 0)
            {
                tellClips.arraySize = TellClipPaths.Length;
                for (var i = 0; i < TellClipPaths.Length; i++)
                {
                    tellClips.GetArrayElementAtIndex(i).objectReferenceValue = LoadMonoClip(TellClipPaths[i]);
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AddTells(ShamblerPrefabPath);
            AddWindUpEyes(ShamblerPrefabPath);

            // Re-save so fields added since the asset was created are written with their defaults.
            EditorUtility.SetDirty(shambler);
            return shambler;
        }

        private static EnemyView LoadOrCreateShamblerPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyView>(ShamblerPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(EnemiesPrefabsFolder);
            var bodyMaterial = LoadOrCreateMaterial("ShamblerBody", LitShaderName, BodyColor);
            var eyeMaterial = LoadOrCreateGlintMaterial(EyeGlintMaterialName, EyeColor);
            var fadeMaterial = LoadOrCreateFadeMaterial("ShamblerFade", BodyColor);

            var shambler = new GameObject("Shambler");

            var agent = shambler.AddComponent<NavMeshAgent>();
            agent.radius = Radius;
            agent.height = Height;
            agent.baseOffset = 0f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;

            var hitbox = shambler.AddComponent<CapsuleCollider>();
            hitbox.radius = Radius;
            hitbox.height = Height;
            hitbox.center = Vector3.up * (Height / 2f);

            var voice = shambler.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 1f;

            var renderers = new[]
            {
                AddPart("Body", PrimitiveType.Capsule, shambler.transform, new Vector3(0f, 0.85f, 0f), new Vector3(12f, 0f, 0f), new Vector3(0.72f, 0.85f, 0.55f), bodyMaterial),
                AddPart("Head", PrimitiveType.Sphere, shambler.transform, new Vector3(0f, 1.58f, 0.2f), Vector3.zero, new Vector3(0.42f, 0.44f, 0.42f), bodyMaterial),
                AddPart("Left Arm", PrimitiveType.Capsule, shambler.transform, new Vector3(-0.36f, 1.22f, 0.32f), new Vector3(75f, 0f, 0f), new Vector3(0.15f, 0.34f, 0.15f), bodyMaterial),
                AddPart("Right Arm", PrimitiveType.Capsule, shambler.transform, new Vector3(0.36f, 1.18f, 0.3f), new Vector3(85f, 0f, 0f), new Vector3(0.15f, 0.34f, 0.15f), bodyMaterial),
                AddPart("Left Eye", PrimitiveType.Sphere, shambler.transform, new Vector3(-0.1f, 1.64f, 0.38f), Vector3.zero, Vector3.one * EyeSize, eyeMaterial),
                AddPart("Right Eye", PrimitiveType.Sphere, shambler.transform, new Vector3(0.1f, 1.64f, 0.38f), Vector3.zero, Vector3.one * EyeSize, eyeMaterial),
            };

            foreach (var eye in new[] { renderers[4], renderers[5] })
            {
                eye.shadowCastingMode = ShadowCastingMode.Off;
                eye.receiveShadows = false;
            }

            var view = shambler.AddComponent<EnemyView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("agent").objectReferenceValue = agent;
            serialized.FindProperty("hitbox").objectReferenceValue = hitbox;
            serialized.FindProperty("voice").objectReferenceValue = voice;
            serialized.FindProperty("fadeMaterial").objectReferenceValue = fadeMaterial;
            var list = serialized.FindProperty("renderers");
            list.arraySize = renderers.Length;
            for (var i = 0; i < renderers.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(shambler, ShamblerPrefabPath);
            Object.DestroyImmediate(shambler);
            return prefab.GetComponent<EnemyView>();
        }

        /// <summary>
        /// Gives the Shambler its Tells, if it hasn't got them: glossy eyes whose emission glints near the character,
        /// and a Tell voice at its head. Works on an existing prefab, so older prefabs are brought up to date in place.
        /// </summary>
        private static void AddTells(string prefabPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyView>(prefabPath);
            if (existing == null || existing.GetComponent<EnemyTellView>() != null)
            {
                return;
            }

            var glintMaterial = LoadOrCreateGlintMaterial(EyeGlintMaterialName, EyeColor);
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var tellVoiceObject = new GameObject("Tell Voice", typeof(AudioSource));
                tellVoiceObject.transform.SetParent(contents.transform, false);
                tellVoiceObject.transform.localPosition = Vector3.up * TellVoiceHeight;
                var tellVoice = tellVoiceObject.GetComponent<AudioSource>();
                tellVoice.playOnAwake = false;
                tellVoice.spatialBlend = 1f;

                var tells = contents.AddComponent<EnemyTellView>();
                var serializedTells = new SerializedObject(tells);
                serializedTells.FindProperty("tellVoice").objectReferenceValue = tellVoice;
                var eyes = serializedTells.FindProperty("eyes");
                eyes.arraySize = EyeNames.Length;
                for (var i = 0; i < EyeNames.Length; i++)
                {
                    var eye = contents.transform.Find(EyeNames[i]).GetComponent<Renderer>();
                    eye.sharedMaterial = glintMaterial;
                    eyes.GetArrayElementAtIndex(i).objectReferenceValue = eye;
                }

                serializedTells.ApplyModifiedPropertiesWithoutUndo();

                var view = new SerializedObject(contents.GetComponent<EnemyView>());
                view.FindProperty("tells").objectReferenceValue = tells;
                view.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>
        /// Wires the Shambler's eyes to flare during its Wind-up, if they aren't already. Works on an existing prefab,
        /// so prefabs built before Wind-ups existed are brought up to date in place.
        /// </summary>
        private static void AddWindUpEyes(string prefabPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyView>(prefabPath);
            if (existing == null || new SerializedObject(existing).FindProperty("windUpRenderers").arraySize > 0)
            {
                return;
            }

            var windUpMaterial = LoadOrCreateMaterial("ShamblerEyesWindUp", UnlitShaderName, WindUpEyeColor);
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var view = contents.GetComponent<EnemyView>();
                var serialized = new SerializedObject(view);
                var renderers = serialized.FindProperty("windUpRenderers");
                renderers.arraySize = EyeNames.Length;
                for (var i = 0; i < EyeNames.Length; i++)
                {
                    renderers.GetArrayElementAtIndex(i).objectReferenceValue = contents.transform.Find(EyeNames[i]).GetComponent<Renderer>();
                }

                serialized.FindProperty("windUpMaterial").objectReferenceValue = windUpMaterial;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void SetClipIfEmpty(SerializedProperty clip, string path)
        {
            if (clip.objectReferenceValue == null)
            {
                clip.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
        }

        private static MeshRenderer AddPart(
            string name,
            PrimitiveType shape,
            Transform parent,
            Vector3 position,
            Vector3 rotation,
            Vector3 scale,
            Material material)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localEulerAngles = rotation;
            part.transform.localScale = scale;
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static Material LoadOrCreateMaterial(string name, string shaderName, Color baseColor)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find(shaderName)) { name = name };
            material.SetColor("_BaseColor", baseColor);
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.1f);
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // Glossy dark eyes that catch the light, with emission the Enemy's Tell view drives at runtime for the glint.
        private static Material LoadOrCreateGlintMaterial(string name, Color baseColor)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find(LitShaderName)) { name = name };
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Smoothness", EyeSmoothness);

            // Rooms have no reflection probes: glossy eyes would mirror the default environment instead of Darkness.
            // Highlights from real lights, such as Lightning, still catch them.
            material.SetFloat("_EnvironmentReflections", 0f);
            material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            material.SetColor("_EmissionColor", Color.black);
            AssetDatabase.CreateAsset(material, path);

            // URP turns emission off on import while its colour is black. Turn it back on afterwards, so the asset
            // keeps the emissive shader variant in builds; the glint sets the colour at runtime.
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        // Tell sounds are positional, and a mono clip places cleanly in 3D.
        private static AudioClip LoadMonoClip(string path)
        {
            if (AssetImporter.GetAtPath(path) is AudioImporter importer && !importer.forceToMono)
            {
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // A transparent Lit material, so a dead Enemy can fade out. Kept as an asset so its shader variant ships.
        private static Material LoadOrCreateFadeMaterial(string name, Color baseColor)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find(LitShaderName)) { name = name };
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Smoothness", 0.1f);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(material, path);
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
    }
}
