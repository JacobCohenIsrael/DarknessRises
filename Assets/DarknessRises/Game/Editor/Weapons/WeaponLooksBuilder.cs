using System.Collections.Generic;
using System.IO;
using DarknessRises.Game.Configuration;
using DarknessRises.Game.Editor.Content;
using DarknessRises.Game.Weapons;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DarknessRises.Game.Editor.Weapons
{
    /// <summary>
    /// Builds how each Primary weapon looks in the character's hands, all from primitives: its model (where the hands
    /// hold it and where its Shots start), its own Muzzle flash, and its icon, a silhouette baked from the model.
    /// Also authors the switch beat's click. A definition that already has a model, flash or icon keeps it, so a
    /// choice made in the Inspector survives a rebuild; delete a prefab or icon to have it built again.
    /// </summary>
    public static class WeaponLooksBuilder
    {
        private const string Root = "Assets/DarknessRises";
        private const string ModelsFolder = Root + "/Prefabs/Weapons/Models";
        private const string FlashesFolder = Root + "/Prefabs/Weapons/MuzzleFlashes";
        private const string IconsFolder = Root + "/Textures/Weapons/Icons";
        private const string MaterialsFolder = Root + "/Materials";
        private const string WeaponMaterialsFolder = MaterialsFolder + "/Weapons";

        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const string ParticlesUnlitShaderName = "Universal Render Pipeline/Particles/Unlit";
        private const string SoftParticleTexture = "Default-Particle.psd";
        private const string HitSparkMaterialPath = MaterialsFolder + "/HitSpark.mat";

        // The click is cut from the reload sequence: the sharp lock at 0.8 s, on its own and with its short ring.
        private const string ClickClipPath = "Assets/Weapons of Choice FREE - Komposite Sound/GUN/Handling_Gun_01_Reload_Sq_SFX.wav";
        private const float ClickStartSeconds = 0.79f;
        private const float ClickSeconds = 0.14f;
        private const float ClickPitch = 1.1f;

        // Icons: a white silhouette of the gun seen from the side, muzzle to the right, on a clear background.
        private const int IconWidth = 256;
        private const int IconHeight = 128;
        private const int IconPadding = 8;
        private const int IconSupersampling = 4;

        private static readonly Color Gunmetal = new(0.12f, 0.12f, 0.13f);
        private static readonly Color Steel = new(0.32f, 0.33f, 0.35f);
        private static readonly Color TankRed = new(0.38f, 0.07f, 0.05f);

        private enum Part
        {
            Body,
            Metal,
            Tank,
        }

        /// <summary>Gives each Primary its model, Muzzle flash and icon, building any that are missing.</summary>
        public static void Author(WeaponDefinition pistol, WeaponDefinition machineGun, WeaponDefinition flamethrower)
        {
            AuthorWeapon(pistol, "Pistol", BuildPistol, BuildPistolFlash, AuthorPistolFlash);
            AuthorWeapon(machineGun, "MachineGun", BuildMachineGun, BuildMachineGunFlash, AuthorMachineGunFlash);
            AuthorWeapon(flamethrower, "Flamethrower", BuildFlamethrower, BuildFlamethrowerFlash, AuthorFlamethrowerFlash);
        }

        /// <summary>Points the switch beat's click at its cut of the reload sequence, unless another was chosen in the Inspector.</summary>
        public static void AuthorSwitch(GameConfiguration configuration)
        {
            var serialized = new SerializedObject(configuration);
            var clip = serialized.FindProperty("weaponSwitch.clip");
            AddressableContent.ReferenceIfEmpty(clip, () =>
            {
                serialized.FindProperty("weaponSwitch.clipStartSeconds").floatValue = ClickStartSeconds;
                serialized.FindProperty("weaponSwitch.clipSeconds").floatValue = ClickSeconds;
                serialized.FindProperty("weaponSwitch.pitch").floatValue = ClickPitch;
                return GameplaySoundImport.Load(ClickClipPath);
            }, AddressableContent.SharedGroup);
            GameplaySoundImport.ApplyToReferenced(clip);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(configuration);
        }

        private static void AuthorWeapon(
            WeaponDefinition definition,
            string name,
            System.Func<GameObject> buildModel,
            System.Func<GameObject> buildFlash,
            System.Action<SerializedObject> authorFlash)
        {
            var serialized = new SerializedObject(definition);
            AddressableContent.ReferenceIfEmpty(
                serialized.FindProperty("model"),
                () => SavePrefab(buildModel(), $"{ModelsFolder}/{name}Model.prefab"),
                AddressableContent.SharedGroup);
            AddressableContent.ReferenceIfEmpty(
                serialized.FindProperty("muzzleFlash.prefab"),
                () =>
                {
                    // A Weapon's first flash of its own comes with its look.
                    authorFlash(serialized);
                    return SavePrefab(buildFlash(), $"{FlashesFolder}/{name}MuzzleFlash.prefab");
                },
                AddressableContent.SharedGroup);

            var icon = serialized.FindProperty("icon");
            if (icon.objectReferenceValue == null)
            {
                var model = AddressableContent.Referenced<GameObject>(serialized.FindProperty("model"));
                icon.objectReferenceValue = model != null ? BakeIcon(model, $"{IconsFolder}/{name}.png") : null;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        // A short white flash.
        private static void AuthorPistolFlash(SerializedObject weapon)
        {
            weapon.FindProperty("muzzleFlash.color").colorValue = new Color(1f, 0.97f, 0.9f);
            weapon.FindProperty("muzzleFlash.seconds").floatValue = 0.06f;
            weapon.FindProperty("muzzleFlash.flicker").floatValue = 0f;
            weapon.FindProperty("muzzleFlash.sparkCount").intValue = 0;
        }

        // A yellower, flickering flash with a few sparks.
        private static void AuthorMachineGunFlash(SerializedObject weapon)
        {
            weapon.FindProperty("muzzleFlash.color").colorValue = new Color(1f, 0.84f, 0.38f);
            weapon.FindProperty("muzzleFlash.flicker").floatValue = 0.6f;
            weapon.FindProperty("muzzleFlash.sparkCount").intValue = 4;
        }

        // No flash: an orange glow at the nozzle, wavering a little like the fire it feeds.
        private static void AuthorFlamethrowerFlash(SerializedObject weapon)
        {
            weapon.FindProperty("muzzleFlash.flicker").floatValue = 0.25f;
            weapon.FindProperty("muzzleFlash.sparkCount").intValue = 0;
        }

        // Short and compact: a slide over a frame, an angled grip and a stub of barrel.
        private static GameObject BuildPistol()
        {
            var model = NewModel("Pistol Model");
            Box(model, "Slide", new Vector3(0f, 0f, 0.43f), new Vector3(0.07f, 0.075f, 0.3f), Part.Metal);
            Box(model, "Frame", new Vector3(0f, -0.05f, 0.41f), new Vector3(0.065f, 0.03f, 0.24f), Part.Body);
            Box(model, "Grip", new Vector3(0f, -0.12f, 0.31f), new Vector3(0.06f, 0.14f, 0.07f), Part.Body, new Vector3(-15f, 0f, 0f));
            CylinderAlongZ(model, "Barrel", new Vector3(0f, 0.005f, 0.595f), 0.035f, 0.03f, Part.Body);
            return Finish(
                model,
                rightGrip: new Vector3(0f, -0.1f, 0.31f),
                leftGrip: new Vector3(-0.045f, -0.11f, 0.33f),
                leftGripTurn: 25f,
                muzzle: new Vector3(0f, 0.005f, 0.62f));
        }

        // Long: a receiver with a stock behind the hands, a shrouded barrel, a foregrip and a drum magazine on the left.
        private static GameObject BuildMachineGun()
        {
            var model = NewModel("Machine Gun Model");
            Box(model, "Receiver", new Vector3(0f, 0f, 0.42f), new Vector3(0.085f, 0.11f, 0.42f), Part.Body);
            Box(model, "Rail", new Vector3(0f, 0.065f, 0.42f), new Vector3(0.03f, 0.02f, 0.36f), Part.Metal);
            Box(model, "Stock", new Vector3(0f, -0.035f, 0.12f), new Vector3(0.065f, 0.1f, 0.22f), Part.Body, new Vector3(5f, 0f, 0f));
            Box(model, "Grip", new Vector3(0f, -0.1f, 0.28f), new Vector3(0.055f, 0.12f, 0.06f), Part.Body, new Vector3(-15f, 0f, 0f));
            Box(model, "Foregrip", new Vector3(0f, -0.09f, 0.6f), new Vector3(0.04f, 0.1f, 0.05f), Part.Body);
            CylinderAlongZ(model, "Shroud", new Vector3(0f, 0.01f, 0.73f), 0.075f, 0.2f, Part.Metal);
            CylinderAlongZ(model, "Barrel", new Vector3(0f, 0.01f, 0.9f), 0.035f, 0.22f, Part.Body);
            CylinderAlongX(model, "Drum", new Vector3(-0.075f, -0.1f, 0.47f), 0.17f, 0.07f, Part.Metal);
            return Finish(
                model,
                rightGrip: new Vector3(0f, -0.1f, 0.28f),
                leftGrip: new Vector3(-0.02f, -0.12f, 0.58f),
                leftGripTurn: 15f,
                muzzle: new Vector3(0f, 0.01f, 1.02f));
        }

        // A tube ending in a nozzle, with a fuel tank on its right and a pilot flame that always glows at the nozzle.
        private static GameObject BuildFlamethrower()
        {
            var model = NewModel("Flamethrower Model");
            CylinderAlongZ(model, "Tube", new Vector3(0f, 0f, 0.45f), 0.09f, 0.5f, Part.Body);
            CylinderAlongZ(model, "Nozzle", new Vector3(0f, 0f, 0.78f), 0.05f, 0.16f, Part.Metal);
            CylinderAlongZ(model, "Nozzle Tip", new Vector3(0f, 0f, 0.87f), 0.075f, 0.025f, Part.Metal);
            Box(model, "Grip", new Vector3(0f, -0.1f, 0.3f), new Vector3(0.05f, 0.12f, 0.06f), Part.Body, new Vector3(-15f, 0f, 0f));
            Box(model, "Foregrip", new Vector3(0f, -0.09f, 0.56f), new Vector3(0.045f, 0.11f, 0.05f), Part.Body);
            CylinderAlongZ(model, "Tank", new Vector3(0.115f, -0.01f, 0.42f), 0.12f, 0.34f, Part.Tank);
            CylinderAlongZ(model, "Tank Cap", new Vector3(0.115f, -0.01f, 0.6f), 0.06f, 0.03f, Part.Metal);
            CylinderAlongX(model, "Hose", new Vector3(0.06f, -0.01f, 0.55f), 0.025f, 0.07f, Part.Metal);

            // Unlit, so it shows in the dark, and soft so it reads as a flame. Not a light: only Lightning casts shadows,
            // and a light this small would cost a light slot for nothing.
            var pilot = Glow(model, "Pilot Flame", new Vector3(0f, -0.03f, 0.91f), new Vector2(0.12f, 0.15f),
                AdditiveMaterial("PilotFlame", new Color(1f, 0.6f, 0.25f, 1f)));
            var built = Finish(
                model,
                rightGrip: new Vector3(0f, -0.1f, 0.3f),
                leftGrip: new Vector3(-0.02f, -0.12f, 0.56f),
                leftGripTurn: 15f,
                muzzle: new Vector3(0f, 0f, 0.89f));
            var serialized = new SerializedObject(built.GetComponent<WeaponModel>());
            serialized.FindProperty("pilotFlame").objectReferenceValue = pilot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return built;
        }

        private static GameObject BuildPistolFlash()
        {
            return NewFlash("Pistol Muzzle Flash", new Vector2(0.55f, 0.8f), 0.12f, AdditiveMaterial("MuzzleFlashWhite", new Color(1f, 0.97f, 0.9f)), sparks: null);
        }

        private static GameObject BuildMachineGunFlash()
        {
            return NewFlash("Machine Gun Muzzle Flash", new Vector2(0.6f, 1f), 0.15f, AdditiveMaterial("MuzzleFlashYellow", new Color(1f, 0.8f, 0.3f)),
                sparks: new Color(1f, 0.85f, 0.4f));
        }

        private static GameObject BuildFlamethrowerFlash()
        {
            return NewFlash("Flamethrower Nozzle Glow", new Vector2(0.7f, 0.7f), 0.05f, AdditiveMaterial("NozzleGlow", new Color(1f, 0.5f, 0.15f, 0.8f)), sparks: null);
        }

        private static GameObject NewModel(string name)
        {
            return new GameObject(name, typeof(WeaponModel));
        }

        private static GameObject Finish(GameObject model, Vector3 rightGrip, Vector3 leftGrip, float leftGripTurn, Vector3 muzzle)
        {
            var right = Point(model, "Right Grip", rightGrip);
            var left = Point(model, "Left Grip", leftGrip);
            left.localEulerAngles = new Vector3(0f, leftGripTurn, 0f);
            var serialized = new SerializedObject(model.GetComponent<WeaponModel>());
            serialized.FindProperty("rightGrip").objectReferenceValue = right;
            serialized.FindProperty("leftGrip").objectReferenceValue = left;
            serialized.FindProperty("muzzle").objectReferenceValue = Point(model, "Muzzle", muzzle);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return model;
        }

        private static Transform Point(GameObject model, string name, Vector3 position)
        {
            var point = new GameObject(name).transform;
            point.SetParent(model.transform, false);
            point.localPosition = position;
            return point;
        }

        private static void Box(GameObject model, string name, Vector3 center, Vector3 size, Part part, Vector3 euler = default)
        {
            var box = Primitive(model, PrimitiveType.Cube, name, part);
            box.localPosition = center;
            box.localEulerAngles = euler;
            box.localScale = size;
        }

        // Unity's cylinder is 2 m tall along y and 1 m across.
        private static void CylinderAlongZ(GameObject model, string name, Vector3 center, float diameter, float length, Part part)
        {
            var cylinder = Primitive(model, PrimitiveType.Cylinder, name, part);
            cylinder.localPosition = center;
            cylinder.localEulerAngles = new Vector3(90f, 0f, 0f);
            cylinder.localScale = new Vector3(diameter, length / 2f, diameter);
        }

        private static void CylinderAlongX(GameObject model, string name, Vector3 center, float diameter, float length, Part part)
        {
            var cylinder = Primitive(model, PrimitiveType.Cylinder, name, part);
            cylinder.localPosition = center;
            cylinder.localEulerAngles = new Vector3(0f, 0f, 90f);
            cylinder.localScale = new Vector3(diameter, length / 2f, diameter);
        }

        private static Transform Primitive(GameObject model, PrimitiveType type, string name, Part part)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            Object.DestroyImmediate(primitive.GetComponent<Collider>());
            primitive.transform.SetParent(model.transform, false);
            primitive.GetComponent<MeshRenderer>().sharedMaterial = part switch
            {
                Part.Metal => LitMaterial("WeaponSteel", Steel),
                Part.Tank => LitMaterial("WeaponTank", TankRed),
                _ => LitMaterial("WeaponGunmetal", Gunmetal),
            };
            return primitive.transform;
        }

        // A soft, unlit glow lying flat, so the top-down camera sees it face on.
        private static Transform Glow(GameObject parent, string name, Vector3 position, Vector2 size, Material material)
        {
            var glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glow.name = name;
            Object.DestroyImmediate(glow.GetComponent<Collider>());
            glow.transform.SetParent(parent.transform, false);
            glow.transform.localPosition = position;
            glow.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            glow.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = glow.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return glow.transform;
        }

        // A shadowless point light (MuzzleFlashView sets it from the Weapon's settings) and an unlit glow just ahead of
        // the muzzle, with sparks thrown forward if it has any.
        private static GameObject NewFlash(string name, Vector2 glowSize, float glowForward, Material glowMaterial, Color? sparks)
        {
            var flash = new GameObject(name, typeof(Light), typeof(MuzzleFlashView));
            var light = flash.GetComponent<Light>();
            light.type = LightType.Point;
            light.shadows = LightShadows.None;
            light.intensity = 0f;

            var glow = Glow(flash, "Glow", Vector3.forward * glowForward, glowSize, glowMaterial);
            var serialized = new SerializedObject(flash.GetComponent<MuzzleFlashView>());
            serialized.FindProperty("flashLight").objectReferenceValue = light;
            serialized.FindProperty("glow").objectReferenceValue = glow;
            if (sparks.HasValue)
            {
                serialized.FindProperty("sparks").objectReferenceValue = BuildSparks(flash.transform, sparks.Value);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return flash;
        }

        private static ParticleSystem BuildSparks(Transform flash, Color color)
        {
            var sparksObject = new GameObject("Sparks", typeof(ParticleSystem));
            sparksObject.transform.SetParent(flash, false);
            var sparks = sparksObject.GetComponent<ParticleSystem>();
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = sparks.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.04f);
            main.startColor = color;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 32;

            var emission = sparks.emission;
            emission.enabled = false;

            // A cone emits along its own forward: the aim.
            var shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.01f;

            var renderer = sparksObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.03f;
            renderer.lengthScale = 1f;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(HitSparkMaterialPath);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return sparks;
        }

        private static GameObject SavePrefab(GameObject built, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var prefab = PrefabUtility.SaveAsPrefabAsset(built, path);
            Object.DestroyImmediate(built);
            return prefab;
        }

        private static Material LitMaterial(string name, Color color)
        {
            var path = $"{WeaponMaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            EnsureFolder(WeaponMaterialsFolder);
            material = new Material(Shader.Find(LitShaderName)) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.35f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // Soft, additive, unlit: it glows on its own, in the dark too.
        private static Material AdditiveMaterial(string name, Color color)
        {
            var path = $"{WeaponMaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            EnsureFolder(WeaponMaterialsFolder);
            material = new Material(Shader.Find(ParticlesUnlitShaderName)) { name = name };
            material.SetTexture("_BaseMap", AssetDatabase.GetBuiltinExtraResource<Texture2D>(SoftParticleTexture));
            material.SetColor("_BaseColor", color);
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
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>
        /// Bakes the model's side-on silhouette (its solid parts, not its pilot flame) into a white sprite with soft
        /// edges, muzzle to the right. It's drawn on the CPU from the meshes, so it needs no camera or GPU.
        /// </summary>
        private static Sprite BakeIcon(GameObject modelPrefab, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                return existing;
            }

            var triangles = SideTriangles(modelPrefab);
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var point in triangles)
            {
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            // Fit the silhouette inside the padding, keeping its proportions, centred.
            var size = max - min;
            var scale = Mathf.Min((IconWidth - 2 * IconPadding) / size.x, (IconHeight - 2 * IconPadding) / size.y);
            var offset = new Vector2(IconWidth, IconHeight) * 0.5f - (min + size * 0.5f) * scale;

            // Each pixel is split into samples; a sample is solid if any triangle covers it, so the parts join without seams.
            const int samplesWide = IconWidth * IconSupersampling;
            const int samplesHigh = IconHeight * IconSupersampling;
            var solid = new bool[samplesWide * samplesHigh];
            var sampleScale = scale * IconSupersampling;
            var sampleOffset = offset * IconSupersampling;
            for (var i = 0; i < triangles.Count; i += 3)
            {
                var a = triangles[i] * sampleScale + sampleOffset;
                var b = triangles[i + 1] * sampleScale + sampleOffset;
                var c = triangles[i + 2] * sampleScale + sampleOffset;
                var area = Cross(b - a, c - a);
                if (Mathf.Abs(area) < 1e-6f)
                {
                    continue;
                }

                var x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
                var x1 = Mathf.Min(samplesWide - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
                var y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
                var y1 = Mathf.Min(samplesHigh - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
                for (var y = y0; y <= y1; y++)
                {
                    for (var x = x0; x <= x1; x++)
                    {
                        if (Inside(new Vector2(x + 0.5f, y + 0.5f), a, b, c, area))
                        {
                            solid[y * samplesWide + x] = true;
                        }
                    }
                }
            }

            const float sampleWeight = 1f / (IconSupersampling * IconSupersampling);
            var pixels = new Color32[IconWidth * IconHeight];
            for (var y = 0; y < IconHeight; y++)
            {
                for (var x = 0; x < IconWidth; x++)
                {
                    var covered = 0;
                    for (var sy = 0; sy < IconSupersampling; sy++)
                    {
                        for (var sx = 0; sx < IconSupersampling; sx++)
                        {
                            if (solid[(y * IconSupersampling + sy) * samplesWide + x * IconSupersampling + sx])
                            {
                                covered++;
                            }
                        }
                    }

                    pixels[y * IconWidth + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(covered * sampleWeight * 255f));
                }
            }

            var texture = new Texture2D(IconWidth, IconHeight, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            EnsureFolder(IconsFolder);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // Every triangle of the model's solid parts, seen from its right side: forward (z) across, up (y) up.
        private static List<Vector2> SideTriangles(GameObject modelPrefab)
        {
            var model = Object.Instantiate(modelPrefab);
            var pilot = model.GetComponent<WeaponModel>().PilotFlame;
            var toModel = model.transform.worldToLocalMatrix;
            var points = new List<Vector2>();
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                if (pilot != null && filter.transform.IsChildOf(pilot))
                {
                    continue;
                }

                var mesh = filter.sharedMesh;
                var vertices = mesh.vertices;
                var indices = mesh.triangles;
                var toModelSpace = toModel * filter.transform.localToWorldMatrix;
                foreach (var index in indices)
                {
                    var vertex = toModelSpace.MultiplyPoint3x4(vertices[index]);
                    points.Add(new Vector2(vertex.z, vertex.y));
                }
            }

            Object.DestroyImmediate(model);
            return points;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private static bool Inside(Vector2 p, Vector2 a, Vector2 b, Vector2 c, float area)
        {
            var sign = Mathf.Sign(area);
            return Cross(b - a, p - a) * sign >= 0f && Cross(c - b, p - b) * sign >= 0f && Cross(a - c, p - c) * sign >= 0f;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
