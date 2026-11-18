using DarknessRises.Game.Debugging;
using DarknessRises.Game.Editor.Content;
using DarknessRises.Game.Weapons;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace DarknessRises.Game.Editor.Weapons
{
    /// <summary>
    /// Builds the Machine gun and Flamethrower definitions, the Flamethrower's fiery Shot prefab, and the debug keys
    /// (1–3) that switch Primaries. New definitions start from placeholder tuning; existing assets are kept, so tuning
    /// done in the Inspector survives a rebuild.
    /// </summary>
    public static class PrimaryWeaponsBuilder
    {
        private const string Root = "Assets/DarknessRises";
        private const string WeaponsConfigurationFolder = Root + "/Configuration/Weapons";
        private const string WeaponsPrefabsFolder = Root + "/Prefabs/Weapons";
        private const string MaterialsFolder = Root + "/Materials";

        public const string MachineGunDefinitionPath = WeaponsConfigurationFolder + "/MachineGun.asset";
        public const string FlamethrowerDefinitionPath = WeaponsConfigurationFolder + "/Flamethrower.asset";
        private const string FlameShotPrefabPath = WeaponsPrefabsFolder + "/FlameShot.prefab";

        // Shared with the Pistol's Shot.
        private const string ShotTrailMaterialPath = MaterialsFolder + "/ShotTrail.mat";
        private const string FlameParticlesMaterialPath = MaterialsFolder + "/FlameParticles.mat";
        private const string ParticlesUnlitShaderName = "Universal Render Pipeline/Particles/Unlit";
        private const string SoftParticleTexture = "Default-Particle.psd";

        // Keys 1-3 equip the Pistol, Machine gun and Flamethrower, in loadout order.
        private const int DebugSwitchKeyCount = 3;

        // Placeholder flame particles, relative to the Shot's size (the particles scale with the Shot): seconds,
        // metres per second and multiples of the Shot's size.
        private const float FlameMinLifetime = 0.12f;
        private const float FlameMaxLifetime = 0.28f;
        private const float FlameMinSpeed = 0.2f;
        private const float FlameMaxSpeed = 1f;
        private const float FlameMinSize = 1.6f;
        private const float FlameMaxSize = 3f;
        private const float FlameEmitterRadius = 0.6f;
        private const float FlamesPerSecond = 60f;
        private const float FlameRise = -0.25f;
        private const int FlameMaxParticles = 24;
        private const float ShotTrailMinVertexDistance = 0.05f;

        // The flame's glowing core: a soft disc lying flat, in multiples of the Shot's size.
        private const float FlameGlowSize = 3f;

        /// <summary>Loads the Machine gun definition, creating it from placeholder tuning if it's missing.</summary>
        public static WeaponDefinition LoadOrCreateMachineGun(WeaponDefinition pistol)
        {
            var machineGun = LoadOrCreate(MachineGunDefinitionPath, out var created);
            var serialized = new SerializedObject(machineGun);
            if (created)
            {
                // The ticket's numbers: a 30-shot magazine, 10 shots per second and a 2.5-second reload.
                serialized.FindProperty("shotsPerSecond").floatValue = 10f;
                serialized.FindProperty("magazineSize").intValue = 30;
                serialized.FindProperty("reloadSeconds").floatValue = 2.5f;
                serialized.FindProperty("shotSpeed").floatValue = 40f;
                serialized.FindProperty("damage").intValue = 1;
                serialized.FindProperty("range").floatValue = 20f;
                serialized.FindProperty("spreadDegrees").floatValue = 4f;
                serialized.FindProperty("knockback.metres").floatValue = 0.12f;
                serialized.FindProperty("knockback.seconds").floatValue = 0.1f;
                serialized.FindProperty("shot.glowColor").colorValue = new Color(1f, 0.88f, 0.55f);
                serialized.FindProperty("shot.size").floatValue = 0.09f;
                serialized.FindProperty("shot.trailSeconds").floatValue = 0.05f;
                serialized.FindProperty("muzzleFlash.intensity").floatValue = 10f;
                serialized.FindProperty("muzzleFlash.range").floatValue = 5f;
                serialized.FindProperty("muzzleFlash.seconds").floatValue = 0.05f;
            }

            FillMissingPrefabs(serialized, () => pistol.Shot.Prefab.editorAsset, () => pistol.HitSpark.Prefab.editorAsset);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(machineGun);
            return machineGun;
        }

        /// <summary>Loads the Flamethrower definition, creating it and its flame Shot from placeholder tuning if they're missing.</summary>
        public static WeaponDefinition LoadOrCreateFlamethrower(WeaponDefinition pistol)
        {
            var flamethrower = LoadOrCreate(FlamethrowerDefinitionPath, out var created);
            var serialized = new SerializedObject(flamethrower);
            if (created)
            {
                var fire = new Color(1f, 0.5f, 0.15f);
                serialized.FindProperty("shotsPerSecond").floatValue = 15f;
                serialized.FindProperty("magazineSize").intValue = 60;
                serialized.FindProperty("reloadSeconds").floatValue = 2.5f;
                serialized.FindProperty("shotSpeed").floatValue = 12f;
                serialized.FindProperty("damage").intValue = 1;
                serialized.FindProperty("range").floatValue = 6f;
                serialized.FindProperty("spreadDegrees").floatValue = 14f;
                // Fifteen hits a second: any push would pin an Enemy against a wall.
                serialized.FindProperty("knockback.metres").floatValue = 0f;
                serialized.FindProperty("shot.glowColor").colorValue = new Color(1f, 0.62f, 0.2f);
                serialized.FindProperty("shot.size").floatValue = 0.2f;
                serialized.FindProperty("shot.trailSeconds").floatValue = 0f;
                serialized.FindProperty("shot.sizeAtRange").floatValue = 3f;
                serialized.FindProperty("shotLight.carriesLight").boolValue = true;
                serialized.FindProperty("shotLight.color").colorValue = fire;
                serialized.FindProperty("shotLight.intensity").floatValue = 5f;
                serialized.FindProperty("shotLight.range").floatValue = 4.5f;
                serialized.FindProperty("shotLight.everyNthShot").intValue = 3;
                serialized.FindProperty("muzzleFlash.color").colorValue = fire;
                serialized.FindProperty("muzzleFlash.intensity").floatValue = 6f;
                serialized.FindProperty("muzzleFlash.range").floatValue = 4f;
                serialized.FindProperty("muzzleFlash.seconds").floatValue = 0.1f;
                serialized.FindProperty("hitSpark.color").colorValue = fire;
                serialized.FindProperty("hitSpark.intensity").floatValue = 2f;
                serialized.FindProperty("hitSpark.sparkCount").intValue = 3;
            }

            FillMissingPrefabs(serialized, () => LoadOrCreateFlameShotPrefab().gameObject, () => pistol.HitSpark.Prefab.editorAsset);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(flamethrower);
            return flamethrower;
        }

        /// <summary>
        /// The debug actions that equip the 1st, 2nd and 3rd Primary. Adds any that are missing to the game's input
        /// actions (Debug/PrimaryWeapon1-3 on keys 1-3) and keeps every other action as it is.
        /// </summary>
        public static InputActionReference[] LoadDebugSwitchActions()
        {
            const string controlsPath = PistolBuilder.ControlsPath;
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(controlsPath);
            var missing = false;
            for (var number = 1; number <= DebugSwitchKeyCount; number++)
            {
                missing |= actions.FindAction(PrimaryWeaponDebugSwitch.ActionPath(number)) == null;
            }

            if (missing)
            {
                var edited = InputActionAsset.FromJson(actions.ToJson());
                var debugMap = edited.FindActionMap(PrimaryWeaponDebugSwitch.MapName)
                    ?? edited.AddActionMap(PrimaryWeaponDebugSwitch.MapName);
                for (var number = 1; number <= DebugSwitchKeyCount; number++)
                {
                    var name = PrimaryWeaponDebugSwitch.ActionPrefix + number;
                    if (debugMap.FindAction(name) == null)
                    {
                        debugMap.AddAction(name, InputActionType.Button, $"<Keyboard>/{number}");
                    }
                }

                System.IO.File.WriteAllText(controlsPath, edited.ToJson());
                Object.DestroyImmediate(edited);
                AssetDatabase.ImportAsset(controlsPath);
                actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(controlsPath);
            }

            var references = new InputActionReference[DebugSwitchKeyCount];
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(controlsPath);
            for (var i = 0; i < references.Length; i++)
            {
                var path = PrimaryWeaponDebugSwitch.ActionPath(i + 1);
                var action = actions.FindAction(path, throwIfNotFound: true);
                foreach (var asset in subAssets)
                {
                    if (asset is InputActionReference reference && reference.action != null && reference.action.id == action.id)
                    {
                        references[i] = reference;
                    }
                }

                if (references[i] == null)
                {
                    throw new System.InvalidOperationException($"No action reference for {path} in {controlsPath}.");
                }
            }

            return references;
        }

        /// <summary>Points the Game root's debug Primary switch at the Debug/PrimaryWeapon1-3 actions.</summary>
        public static void WireDebugSwitch(GameRoot gameRoot)
        {
            var references = LoadDebugSwitchActions();
            var serialized = new SerializedObject(gameRoot);
            var property = serialized.FindProperty("switchPrimaryWeapon");
            property.arraySize = references.Length;
            for (var i = 0; i < references.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = references[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static WeaponDefinition LoadOrCreate(string path, out bool created)
        {
            var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            created = definition == null;
            if (created)
            {
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            return definition;
        }

        // Loaded through Addressables with the character's other content.
        private static void FillMissingPrefabs(SerializedObject definition, System.Func<Object> shot, System.Func<Object> hitSpark)
        {
            AddressableContent.ReferenceIfEmpty(definition.FindProperty("shot.prefab"), shot, AddressableContent.SharedGroup);
            AddressableContent.ReferenceIfEmpty(definition.FindProperty("hitSpark.prefab"), hitSpark, AddressableContent.SharedGroup);
        }

        // A soft, additive glow lying flat, a shadowless light the Weapon turns on for some Shots,
        // and soft additive flames left behind in the air as it flies. No solid body, so it reads as fire.
        private static ShotView LoadOrCreateFlameShotPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ShotView>(FlameShotPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            var shot = new GameObject("Flame Shot");

            var glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glow.name = "Glow";
            Object.DestroyImmediate(glow.GetComponent<Collider>());
            glow.transform.SetParent(shot.transform, false);
            glow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            glow.transform.localScale = Vector3.one * FlameGlowSize;
            var body = glow.GetComponent<MeshRenderer>();
            body.sharedMaterial = LoadOrCreateFlameParticlesMaterial();
            body.shadowCastingMode = ShadowCastingMode.Off;
            body.receiveShadows = false;

            var trail = shot.AddComponent<TrailRenderer>();
            trail.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(ShotTrailMaterialPath);
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.minVertexDistance = ShotTrailMinVertexDistance;
            trail.autodestruct = false;

            var lightObject = new GameObject("Light", typeof(Light));
            lightObject.transform.SetParent(shot.transform, false);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Point;
            light.shadows = LightShadows.None;
            light.enabled = false;

            var flames = BuildFlameParticles(shot.transform);

            var view = shot.AddComponent<ShotView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("body").objectReferenceValue = body;
            serialized.FindProperty("trail").objectReferenceValue = trail;
            serialized.FindProperty("shotLight").objectReferenceValue = light;
            serialized.FindProperty("particles").objectReferenceValue = flames;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(shot, FlameShotPrefabPath);
            Object.DestroyImmediate(shot);
            return prefab.GetComponent<ShotView>();
        }

        private static ParticleSystem BuildFlameParticles(Transform shot)
        {
            var flamesObject = new GameObject("Flames", typeof(ParticleSystem));
            flamesObject.transform.SetParent(shot, false);
            var flames = flamesObject.GetComponent<ParticleSystem>();
            flames.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = flames.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(FlameMinLifetime, FlameMaxLifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(FlameMinSpeed, FlameMaxSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(FlameMinSize, FlameMaxSize);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.4f), new Color(1f, 0.4f, 0.1f));
            main.gravityModifier = FlameRise;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = FlameMaxParticles;

            var emission = flames.emission;
            emission.rateOverTime = FlamesPerSecond;

            var shape = flames.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = FlameEmitterRadius;

            var colorOverLifetime = flames.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.9f, 0.6f), 0f),
                    new GradientColorKey(new Color(1f, 0.45f, 0.1f), 0.4f),
                    new GradientColorKey(new Color(0.5f, 0.08f, 0.02f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(0.5f, 0.6f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = flames.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));

            var renderer = flamesObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = LoadOrCreateFlameParticlesMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return flames;
        }

        // Soft, additive, unlit particles: they glow on their own and brighten where they overlap.
        private static Material LoadOrCreateFlameParticlesMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(FlameParticlesMaterialPath);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find(ParticlesUnlitShaderName)) { name = "FlameParticles" };
            material.SetTexture("_BaseMap", AssetDatabase.GetBuiltinExtraResource<Texture2D>(SoftParticleTexture));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(material, FlameParticlesMaterialPath);
            return material;
        }
    }
}
