using DarknessRises.Game.Editor.Content;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Enemies.Tells;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace DarknessRises.Game.Editor.Enemies
{
    /// <summary>
    /// Builds the Enemy types: each one's Enemy definition asset and a placeholder prefab with eyes that glint as a Tell.
    /// The Shambler is a hunched figure with reaching arms; the Runner a smaller, paler figure leaning into a sprint.
    /// Existing assets are kept, so tuning done in the Inspector survives a rebuild.
    /// </summary>
    public static class EnemyBuilder
    {
        private const string Root = "Assets/DarknessRises";
        private const string EnemiesConfigurationFolder = Root + "/Configuration/Enemies";
        private const string EnemiesPrefabsFolder = Root + "/Prefabs/Enemies";
        private const string MaterialsFolder = Root + "/Materials";

        public const string ShamblerDefinitionPath = EnemiesConfigurationFolder + "/Shambler.asset";
        private const string ShamblerPrefabPath = EnemiesPrefabsFolder + "/Shambler.prefab";

        public const string RunnerDefinitionPath = EnemiesConfigurationFolder + "/Runner.asset";
        private const string RunnerPrefabPath = EnemiesPrefabsFolder + "/Runner.prefab";

        // Approved Free Pack placeholders.
        private const string HurtClipPath = "Assets/Free Pack/Bullet Impact 14.wav";
        private const string DeathClipPath = "Assets/Free Pack/Bloody punch.wav";
        private const string WindUpClipPath = "Assets/Free Pack/Magic Spell_Short Reverse_1.wav";
        private const string AttackClipPath = "Assets/Free Pack/Magic Spell_Simple Swoosh_6.wav";
        private static readonly string[] TellClipPaths = { "Assets/Free Pack/Monster Bite.wav" };

        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const string UnlitShaderName = "Universal Render Pipeline/Unlit";

        private const float EyeSize = 0.11f;
        private static readonly Color EyeColor = new(0.05f, 0.03f, 0.02f);
        private const float EyeSmoothness = 0.9f;
        private static readonly Color WindUpEyeColor = new(8f, 0.4f, 0.2f);
        private static readonly string[] EyeNames = { "Left Eye", "Right Eye" };
        private const string EyeGlintMaterialName = "ShamblerEyeGlint";

        // Placeholder Shambler: hunched, with reaching arms. In metres and degrees.
        private static readonly Figure Shambler = new(
            "Shambler",
            ShamblerPrefabPath,
            height: 1.8f,
            radius: 0.4f,
            bodyColor: new Color(0.2f, 0.24f, 0.19f),
            tellVoiceHeight: 1.6f,
            new[]
            {
                new Part("Body", PrimitiveType.Capsule, new Vector3(0f, 0.85f, 0f), new Vector3(12f, 0f, 0f), new Vector3(0.72f, 0.85f, 0.55f)),
                new Part("Head", PrimitiveType.Sphere, new Vector3(0f, 1.58f, 0.2f), Vector3.zero, new Vector3(0.42f, 0.44f, 0.42f)),
                new Part("Left Arm", PrimitiveType.Capsule, new Vector3(-0.36f, 1.22f, 0.32f), new Vector3(75f, 0f, 0f), new Vector3(0.15f, 0.34f, 0.15f)),
                new Part("Right Arm", PrimitiveType.Capsule, new Vector3(0.36f, 1.18f, 0.3f), new Vector3(85f, 0f, 0f), new Vector3(0.15f, 0.34f, 0.15f)),
            },
            new[]
            {
                new Vector3(-0.1f, 1.64f, 0.38f),
                new Vector3(0.1f, 1.64f, 0.38f),
            },
            EyeSize);

        // Placeholder Runner: smaller and paler than the Shambler, leaning into a sprint with its arms swept back.
        private static readonly Figure Runner = new(
            "Runner",
            RunnerPrefabPath,
            height: 1.55f,
            radius: 0.3f,
            bodyColor: new Color(0.36f, 0.31f, 0.27f),
            tellVoiceHeight: 1.4f,
            new[]
            {
                new Part("Body", PrimitiveType.Capsule, new Vector3(0f, 0.98f, 0.08f), new Vector3(32f, 0f, 0f), new Vector3(0.46f, 0.52f, 0.36f)),
                new Part("Head", PrimitiveType.Sphere, new Vector3(0f, 1.38f, 0.4f), Vector3.zero, new Vector3(0.3f, 0.32f, 0.32f)),
                new Part("Left Arm", PrimitiveType.Capsule, new Vector3(-0.26f, 1.02f, -0.1f), new Vector3(-40f, 0f, 0f), new Vector3(0.1f, 0.3f, 0.1f)),
                new Part("Right Arm", PrimitiveType.Capsule, new Vector3(0.26f, 1.02f, -0.08f), new Vector3(-50f, 0f, 0f), new Vector3(0.1f, 0.3f, 0.1f)),
                new Part("Left Leg", PrimitiveType.Capsule, new Vector3(-0.12f, 0.38f, 0.1f), new Vector3(25f, 0f, 0f), new Vector3(0.13f, 0.38f, 0.13f)),
                new Part("Right Leg", PrimitiveType.Capsule, new Vector3(0.12f, 0.38f, -0.08f), new Vector3(-20f, 0f, 0f), new Vector3(0.13f, 0.38f, 0.13f)),
            },
            new[]
            {
                new Vector3(-0.07f, 1.42f, 0.54f),
                new Vector3(0.07f, 1.42f, 0.54f),
            },
            eyeSize: 0.09f);

        // The Runner's tuning, set once when its definition is created: fast and fragile, faster than the character walks.
        private const int RunnerHitPoints = 2;
        private const float RunnerSpeed = 4.2f;
        private const float RunnerAcceleration = 14f;
        private const float RunnerTurnSpeed = 420f;
        private const float RunnerPathRefreshSeconds = 0.15f;

        // Light on its feet: hits push it further than a Shambler.
        private const float RunnerKnockbackMultiplier = 1.3f;

        /// <summary>Loads the Shambler definition, creating it and its prefab if they're missing.</summary>
        public static EnemyDefinition LoadOrCreateShambler()
        {
            // The definition's defaults are the Shambler: slow and tough, slower than the character walks.
            return LoadOrCreateEnemy(ShamblerDefinitionPath, Shambler, _ => { });
        }

        /// <summary>Loads the Runner definition, creating it and its prefab if they're missing.</summary>
        public static EnemyDefinition LoadOrCreateRunner()
        {
            return LoadOrCreateEnemy(RunnerDefinitionPath, Runner, created =>
            {
                created.FindProperty("hitPoints").intValue = RunnerHitPoints;
                created.FindProperty("speed").floatValue = RunnerSpeed;
                created.FindProperty("acceleration").floatValue = RunnerAcceleration;
                created.FindProperty("turnSpeed").floatValue = RunnerTurnSpeed;
                created.FindProperty("pathRefreshSeconds").floatValue = RunnerPathRefreshSeconds;
                created.FindProperty("knockback.multiplier").floatValue = RunnerKnockbackMultiplier;
            });
        }

        // Loads an Enemy type's definition, creating it (tuned by <paramref name="tuneCreated"/>) and its prefab if missing.
        private static EnemyDefinition LoadOrCreateEnemy(string definitionPath, Figure figure, System.Action<SerializedObject> tuneCreated)
        {
            EnsureFolder(EnemiesConfigurationFolder);
            var enemyType = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(definitionPath);
            var serialized = default(SerializedObject);
            if (enemyType == null)
            {
                enemyType = ScriptableObject.CreateInstance<EnemyDefinition>();
                AssetDatabase.CreateAsset(enemyType, definitionPath);
                serialized = new SerializedObject(enemyType);
                tuneCreated(serialized);
            }

            serialized ??= new SerializedObject(enemyType);

            // Loaded through Addressables, so each Level loads only the Enemy types it uses.
            AddressableContent.ReferenceIfEmpty(
                serialized.FindProperty("prefab"),
                () => LoadOrCreatePrefab(figure).gameObject,
                AddressableContent.EnemiesGroup);
            ReferenceClipIfEmpty(serialized.FindProperty("audio.spawnClip"), null);
            ReferenceClipIfEmpty(serialized.FindProperty("audio.hurtClip"), HurtClipPath);
            ReferenceClipIfEmpty(serialized.FindProperty("audio.deathClip"), DeathClipPath);
            ReferenceClipIfEmpty(serialized.FindProperty("audio.windUpClip"), WindUpClipPath);
            ReferenceClipIfEmpty(serialized.FindProperty("audio.attackClip"), AttackClipPath);

            // Tell sounds were added after the Shambler; give an older asset the placeholders once.
            var tellClips = serialized.FindProperty("tells.sounds.clips");
            if (tellClips.arraySize == 0)
            {
                tellClips.arraySize = TellClipPaths.Length;
            }

            for (var i = 0; i < tellClips.arraySize; i++)
            {
                var path = i < TellClipPaths.Length ? TellClipPaths[i] : null;
                AddressableContent.ReferenceIfEmpty(
                    tellClips.GetArrayElementAtIndex(i),
                    () => path != null ? LoadMonoClip(path) : null,
                    AddressableContent.EnemiesGroup);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AddTells(figure.PrefabPath, figure.TellVoiceHeight);
            AddWindUpEyes(figure.PrefabPath);

            // Re-save so fields added since the asset was created are written with their defaults.
            EditorUtility.SetDirty(enemyType);
            return enemyType;
        }

        private static EnemyView LoadOrCreatePrefab(Figure figure)
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyView>(figure.PrefabPath);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(EnemiesPrefabsFolder);
            var bodyMaterial = LoadOrCreateMaterial($"{figure.Name}Body", LitShaderName, figure.BodyColor);
            var eyeMaterial = LoadOrCreateGlintMaterial(EyeGlintMaterialName, EyeColor);
            var fadeMaterial = LoadOrCreateFadeMaterial($"{figure.Name}Fade", figure.BodyColor);

            var root = new GameObject(figure.Name);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = figure.Radius;
            agent.height = figure.Height;
            agent.baseOffset = 0f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;

            var hitbox = root.AddComponent<CapsuleCollider>();
            hitbox.radius = figure.Radius;
            hitbox.height = figure.Height;
            hitbox.center = Vector3.up * (figure.Height / 2f);

            var voice = root.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 1f;

            var renderers = new MeshRenderer[figure.Parts.Length + EyeNames.Length];
            for (var i = 0; i < figure.Parts.Length; i++)
            {
                var part = figure.Parts[i];
                renderers[i] = AddPart(part.Name, part.Shape, root.transform, part.Position, part.Rotation, part.Scale, bodyMaterial);
            }

            for (var i = 0; i < EyeNames.Length; i++)
            {
                var eye = AddPart(EyeNames[i], PrimitiveType.Sphere, root.transform, figure.Eyes[i], Vector3.zero, Vector3.one * figure.EyeSize, eyeMaterial);
                eye.shadowCastingMode = ShadowCastingMode.Off;
                eye.receiveShadows = false;
                renderers[figure.Parts.Length + i] = eye;
            }

            var view = root.AddComponent<EnemyView>();
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

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, figure.PrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<EnemyView>();
        }

        // One primitive of a placeholder figure, in metres and degrees.
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

        // An Enemy type's placeholder figure: its body parts, then its two eyes.
        private sealed class Figure
        {
            public Figure(
                string name,
                string prefabPath,
                float height,
                float radius,
                Color bodyColor,
                float tellVoiceHeight,
                Part[] parts,
                Vector3[] eyes,
                float eyeSize)
            {
                Name = name;
                PrefabPath = prefabPath;
                Height = height;
                Radius = radius;
                BodyColor = bodyColor;
                TellVoiceHeight = tellVoiceHeight;
                Parts = parts;
                Eyes = eyes;
                EyeSize = eyeSize;
            }

            public string Name { get; }
            public string PrefabPath { get; }
            public float Height { get; }
            public float Radius { get; }
            public Color BodyColor { get; }
            public float TellVoiceHeight { get; }
            public Part[] Parts { get; }
            public Vector3[] Eyes { get; }
            public float EyeSize { get; }
        }


        /// <summary>
        /// Gives an Enemy type its Tells, if it hasn't got them: glossy eyes whose emission glints near the character,
        /// and a Tell voice at its head. Works on an existing prefab, so older prefabs are brought up to date in place.
        /// </summary>
        private static void AddTells(string prefabPath, float tellVoiceHeight)
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
                tellVoiceObject.transform.localPosition = Vector3.up * tellVoiceHeight;
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
        /// Wires an Enemy type's eyes to flare during its Wind-up, if they aren't already. Works on an existing prefab,
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

        // An empty clip gets the placeholder at <paramref name="path"/> (none if null); a set one stays as it is.
        private static void ReferenceClipIfEmpty(SerializedProperty clip, string path)
        {
            if (path == null && AddressableContent.Referenced<AudioClip>(clip) == null)
            {
                return;
            }

            AddressableContent.ReferenceIfEmpty(clip, () => AssetDatabase.LoadAssetAtPath<AudioClip>(path), AddressableContent.EnemiesGroup);
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
