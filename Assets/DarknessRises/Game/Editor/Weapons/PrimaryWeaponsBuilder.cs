using DarknessRises.Game.Controls;
using DarknessRises.Game.Editor.Content;
using DarknessRises.Game.Weapons;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace DarknessRises.Game.Editor.Weapons
{
    /// <summary>
    /// Builds the Machine gun and Flamethrower definitions, the Flamethrower's fiery Shot prefab, and the Gameplay
    /// actions that switch Primaries (keys 1–3, the mouse wheel, and the touch Weapon button's control). New
    /// definitions start from placeholder tuning; existing assets are kept, so tuning done in the Inspector survives
    /// a rebuild.
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

        // Keys 1-3 equip the Pistol, Machine gun and Flamethrower: their places in the Shop's order.
        private const int WeaponKeyCount = 3;
        private const string GameplayMap = "Gameplay";
        private const string DebugMap = "Debug";
        private const string WeaponActionPrefix = "Weapon";
        private const string NextWeaponAction = "NextWeapon";
        private const string PreviousWeaponAction = "PreviousWeapon";
        private const string KeyboardMouseGroup = "Keyboard&Mouse";
        private const string TouchGroup = "Touch";

        // The debug-only actions the Weapon actions replace.
        private const string FormerDebugActionPrefix = "PrimaryWeapon";

        // First-pass ammo reserves: what a gun carries beyond its magazine. The Machine gun's 240 is eight magazines,
        // about 27 seconds of held fire; the Flamethrower's 300 is five tanks, 20 seconds. Enough to lean on for the
        // hard part of a Level, not to carry a whole one: the Pistol never runs out.
        private const int MachineGunReserve = 240;
        private const int FlamethrowerReserve = 300;

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
            AuthorReserve(serialized, MachineGunReserve);
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
            AuthorReserve(serialized, FlamethrowerReserve);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(flamethrower);
            return flamethrower;
        }

        // Gives a Weapon that was never given one a finite ammo reserve. Unlimited ammo with a reserve of 0 is how a
        // definition starts; once either was set in the Inspector, it's left alone.
        private static void AuthorReserve(SerializedObject weapon, int reserve)
        {
            var unlimited = weapon.FindProperty("hasUnlimitedAmmo");
            var rounds = weapon.FindProperty("ammoReserve");
            if (unlimited.boolValue && rounds.intValue == 0)
            {
                unlimited.boolValue = false;
                rounds.intValue = reserve;
            }
        }

        /// <summary>
        /// Points the player's controls at the Gameplay actions that switch Primaries, adding them to the game's input
        /// actions if they're missing: Weapon1-3 on keys 1-3, NextWeapon on the mouse wheel down and on the Gesture
        /// device (the touch Weapon button presses it), PreviousWeapon on the wheel up. The debug-only
        /// PrimaryWeapon1-3 actions they replace are removed. Every other action is kept as it is.
        /// </summary>
        public static void WireSwitch(PlayerControls controls)
        {
            EnsureSwitchActions();
            var serialized = new SerializedObject(controls);
            var select = serialized.FindProperty("selectWeapon");
            select.arraySize = WeaponKeyCount;
            for (var number = 1; number <= WeaponKeyCount; number++)
            {
                select.GetArrayElementAtIndex(number - 1).objectReferenceValue = LoadReference($"{GameplayMap}/{WeaponActionPrefix}{number}");
            }

            serialized.FindProperty("nextWeapon").objectReferenceValue = LoadReference($"{GameplayMap}/{NextWeaponAction}");
            serialized.FindProperty("previousWeapon").objectReferenceValue = LoadReference($"{GameplayMap}/{PreviousWeaponAction}");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureSwitchActions()
        {
            const string controlsPath = PistolBuilder.ControlsPath;
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(controlsPath);
            var isUpToDate = actions.FindAction($"{GameplayMap}/{NextWeaponAction}") != null
                && actions.FindAction($"{GameplayMap}/{PreviousWeaponAction}") != null;
            for (var number = 1; number <= WeaponKeyCount; number++)
            {
                isUpToDate &= actions.FindAction($"{GameplayMap}/{WeaponActionPrefix}{number}") != null;
                isUpToDate &= actions.FindAction($"{DebugMap}/{FormerDebugActionPrefix}{number}") == null;
            }

            if (isUpToDate)
            {
                return;
            }

            var edited = InputActionAsset.FromJson(actions.ToJson());
            var gameplay = edited.FindActionMap(GameplayMap, throwIfNotFound: true);
            var debug = edited.FindActionMap(DebugMap);
            for (var number = 1; number <= WeaponKeyCount; number++)
            {
                debug?.FindAction(FormerDebugActionPrefix + number)?.RemoveAction();
                var name = WeaponActionPrefix + number;
                if (gameplay.FindAction(name) == null)
                {
                    gameplay.AddAction(name, InputActionType.Button, $"<Keyboard>/{number}", groups: KeyboardMouseGroup);
                }
            }

            if (gameplay.FindAction(NextWeaponAction) == null)
            {
                var next = gameplay.AddAction(NextWeaponAction, InputActionType.Button, "<Mouse>/scroll/down", groups: KeyboardMouseGroup);
                next.AddBinding("<GestureDevice>/nextWeapon", groups: TouchGroup);
            }

            if (gameplay.FindAction(PreviousWeaponAction) == null)
            {
                gameplay.AddAction(PreviousWeaponAction, InputActionType.Button, "<Mouse>/scroll/up", groups: KeyboardMouseGroup);
            }

            System.IO.File.WriteAllText(controlsPath, edited.ToJson());
            Object.DestroyImmediate(edited);
            AssetDatabase.ImportAsset(controlsPath);
        }

        private static InputActionReference LoadReference(string actionPath)
        {
            const string controlsPath = PistolBuilder.ControlsPath;
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(controlsPath);
            var action = actions.FindAction(actionPath, throwIfNotFound: true);
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(controlsPath))
            {
                if (asset is InputActionReference reference && reference.action != null && reference.action.id == action.id)
                {
                    return reference;
                }
            }

            throw new System.InvalidOperationException($"No action reference for {actionPath} in {controlsPath}.");
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
