using DarknessRises.Game.Controls;
using DarknessRises.Game.Editor.Content;
using DarknessRises.Game.Editor.Health;
using DarknessRises.Game.Grenades;
using DarknessRises.Game.Hud;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace DarknessRises.Game.Editor.Grenades
{
    /// <summary>
    /// Builds the Grenade: its definition asset and pooled Grenade and explosion prefabs, the thrower on the
    /// character, the charges on the HUD, and the camera's impulse listener for the explosion's shake.
    /// Existing assets are kept, so tuning done in the Inspector survives a rebuild.
    /// </summary>
    public static class GrenadeBuilder
    {
        private const string Root = "Assets/DarknessRises";
        private const string WeaponsConfigurationFolder = Root + "/Configuration/Weapons";
        private const string WeaponsPrefabsFolder = Root + "/Prefabs/Weapons";
        private const string MaterialsFolder = Root + "/Materials";
        private const string ControlsPath = Root + "/Input/Controls.inputactions";

        public const string GrenadeDefinitionPath = WeaponsConfigurationFolder + "/Grenade.asset";
        private const string GrenadePrefabPath = WeaponsPrefabsFolder + "/Grenade.prefab";
        private const string ExplosionPrefabPath = WeaponsPrefabsFolder + "/GrenadeExplosion.prefab";

        // Placeholder bang from the old Free Pack sounds (approved as placeholders).
        // Grenade3 (Grenade Sound FX, MGWSoundDesign), approved for the game. It builds up for about half a second first.
        private const string ExplosionClipPath = "Assets/Grenade Sound FX/Grenade/Grenade3.wav";

        // The Free Pack placeholder it replaced. Nothing else uses it.
        private const string FormerExplosionClipPath = "Assets/Free Pack/Explosion 2.wav";

        private const string GrenadeActionPath = "Gameplay/Grenade";

        private const string UnlitShaderName = "Universal Render Pipeline/Unlit";
        private const string ParticlesUnlitShaderName = "Universal Render Pipeline/Particles/Unlit";
        private const string DefaultParticleMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat";
        private const string PipSpritePath = "UI/Skin/Knob.psd";

        // Where the Grenade leaves the hand, relative to the Weapon mount, in metres.
        private static readonly Vector3 HandOffset = new(0.25f, 0.2f, 0.55f);
        private const float TrailMinVertexDistance = 0.05f;

        // Placeholder explosion particles: seconds, metres per second and metres.
        private const float FireballMinLifetime = 0.35f;
        private const float FireballMaxLifetime = 0.7f;
        private const float FireballMinSpeed = 1.5f;
        private const float FireballMaxSpeed = 5f;
        private const float FireballMinSize = 0.6f;
        private const float FireballMaxSize = 1.5f;
        private const float FireballSpread = 0.4f;
        private const float FireballRise = -0.15f;
        private const float FireballGrowth = 1.8f;
        private const float FireballDrag = 0.15f;
        private const int FireballMaxParticles = 64;

        private const float SparkMinLifetime = 0.4f;
        private const float SparkMaxLifetime = 0.9f;
        private const float SparkMinSpeed = 6f;
        private const float SparkMaxSpeed = 14f;
        private const float SparkMinSize = 0.04f;
        private const float SparkMaxSize = 0.09f;
        private const float SparkGravity = 1.2f;
        private const float SparkStretch = 0.05f;
        private const float SparkSpread = 0.1f;
        private const int SparkMaxParticles = 128;

        private static readonly Color FireballHot = new(1f, 0.92f, 0.65f, 1f);
        private static readonly Color FireballWarm = new(1f, 0.5f, 0.15f, 0.85f);
        private static readonly Color FireballCool = new(0.45f, 0.08f, 0.02f, 0f);
        private static readonly Color SparkColor = new(1f, 0.7f, 0.3f, 1f);

        // HUD: a row of pips above the ammo readout, right-aligned with it. Reference pixels at 1920x1080.
        private static readonly Vector2 GrenadesMargin = new(56f, 186f);
        private static readonly Vector2 GrenadesSize = new(360f, 44f);
        private const float PipSize = 40f;
        private const float PipSpacing = 14f;
        private static readonly Color PipEmptyColor = new(0.85f, 0.85f, 0.85f, 0.22f);
        private static readonly Color PipFullColor = new(1f, 0.6f, 0.25f, 0.9f);

        // The touch Grenade button's contents, in HUD reference pixels. Its size, place and colours at runtime come
        // from the Grenade button settings; these only lay out the label and pips inside it.
        private const float ButtonPlaceholderSize = 150f;
        private static readonly Color ButtonFaceColor = new(0.16f, 0.16f, 0.17f, 0.6f);
        private const float ButtonLabelFontSize = 22f;
        private static readonly Color ButtonLabelColor = new(0.9f, 0.9f, 0.9f, 0.9f);
        private static readonly Vector2 ButtonLabelOffset = new(0f, 18f);
        private static readonly Vector2 ButtonLabelSize = new(140f, 30f);
        private static readonly Vector2 ButtonPipsSize = new(110f, 26f);
        private static readonly Vector2 ButtonPipsOffset = new(0f, -20f);
        private const float ButtonPipSize = 24f;
        private const float ButtonPipSpacing = 8f;

        /// <summary>Loads the Grenade definition, creating it and its prefabs if they're missing.</summary>
        public static GrenadeDefinition LoadOrCreateGrenade()
        {
            EnsureFolder(WeaponsConfigurationFolder);
            var grenade = AssetDatabase.LoadAssetAtPath<GrenadeDefinition>(GrenadeDefinitionPath);
            if (grenade == null)
            {
                // The definition's defaults are the Grenade: 3 charges, one refilling every 20 seconds.
                grenade = ScriptableObject.CreateInstance<GrenadeDefinition>();
                AssetDatabase.CreateAsset(grenade, GrenadeDefinitionPath);
            }

            var serialized = new SerializedObject(grenade);
            // Loaded through Addressables with the character's other content.
            const string group = AddressableContent.SharedGroup;
            AddressableContent.ReferenceIfEmpty(serialized.FindProperty("body.prefab"), () => LoadOrCreateGrenadePrefab().gameObject, group);
            AddressableContent.ReferenceIfEmpty(serialized.FindProperty("explosion.prefab"), () => LoadOrCreateExplosionPrefab().gameObject, group);
            var explosionClip = serialized.FindProperty("explosion.clip");
            var referencedClip = AddressableContent.Referenced<AudioClip>(explosionClip);
            if (referencedClip != null && AssetDatabase.GetAssetPath(referencedClip) == FormerExplosionClipPath)
            {
                // The former placeholder leaves the content too, so no bundle carries it.
                AddressableContent.Setup().RemoveAssetEntry(AssetDatabase.AssetPathToGUID(FormerExplosionClipPath));
                AddressableContent.Reference(explosionClip, GameplaySoundImport.Load(ExplosionClipPath), group);
            }
            else
            {
                AddressableContent.ReferenceIfEmpty(explosionClip, () => GameplaySoundImport.Load(ExplosionClipPath), group);
            }

            GameplaySoundImport.ApplyToReferenced(explosionClip);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Re-save so fields added since the asset was created are written with their defaults.
            EditorUtility.SetDirty(grenade);
            return grenade;
        }

        /// <summary>
        /// Gives the character its Grenade thrower, puts the charges on the HUD, lets the camera feel the explosion's
        /// impulse, and hands the thrower and HUD to the Game Root. Call after the Primary weapon and the HUD exist.
        /// </summary>
        public static void Build(GameRoot gameRoot, Transform character)
        {
            var controls = character.GetComponent<PlayerControls>();
            var serializedControls = new SerializedObject(controls);
            serializedControls.FindProperty("grenade").objectReferenceValue = LoadActionReference(GrenadeActionPath);
            serializedControls.ApplyModifiedPropertiesWithoutUndo();

            var hand = new GameObject("Grenade Hand").transform;
            hand.SetParent(character.Find("Weapon Mount"), false);
            hand.localPosition = HandOffset;

            var thrower = character.gameObject.AddComponent<GrenadeThrowerView>();
            var serializedThrower = new SerializedObject(thrower);
            serializedThrower.FindProperty("controls").objectReferenceValue = controls;
            serializedThrower.FindProperty("hand").objectReferenceValue = hand;
            serializedThrower.FindProperty("floor").objectReferenceValue = character;
            serializedThrower.ApplyModifiedPropertiesWithoutUndo();

            AddImpulseListener();

            var serializedRoot = new SerializedObject(gameRoot);
            serializedRoot.FindProperty("grenadeThrower").objectReferenceValue = thrower;
            var cornerCharges = BuildGrenadeHud(GrenadesMargin, GrenadesSize, PipSize, PipSpacing, TextAnchor.LowerRight);
            serializedRoot.FindProperty("grenadeHud").objectReferenceValue = cornerCharges;
            serializedRoot.FindProperty("grenadeButton").objectReferenceValue = BuildGrenadeButton(controls, cornerCharges.gameObject);
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddImpulseListener()
        {
            var camera = Object.FindAnyObjectByType<CinemachineCamera>();
            var listener = camera.GetComponent<CinemachineImpulseListener>();
            if (listener == null)
            {
                listener = camera.gameObject.AddComponent<CinemachineImpulseListener>();
            }

            // Cinemachine's own defaults, set explicitly: a component added from code may skip Reset.
            listener.ApplyAfter = CinemachineCore.Stage.Noise;
            listener.ChannelMask = 1;
            listener.Gain = 1f;
            listener.Use2DDistance = false;
            listener.UseCameraSpace = true;
            listener.ReactionSettings = new CinemachineImpulseListener.ImpulseReaction
            {
                AmplitudeGain = 1f,
                FrequencyGain = 1f,
                Duration = 1f,
            };
            EditorUtility.SetDirty(listener);
        }

        // The touch Grenade button: a round, interactable face with a label and its own row of charge pips.
        // The view sits on the button itself: its Selectable takes the pointer events, which never bubble past it.
        private static GrenadeButtonView BuildGrenadeButton(PlayerControls controls, GameObject cornerCharges)
        {
            var hud = GameObject.Find("HUD");
            if (!hud.TryGetComponent<GraphicRaycaster>(out _))
            {
                // The button is the HUD's first control: the UI and the Gesture input's UI filter must be able to hit it.
                hud.AddComponent<GraphicRaycaster>();
            }

            // Covers the canvas, so the button's anchored position is its screen position over the canvas scale.
            var root = new GameObject("Grenade Button Area", typeof(RectTransform));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.SetParent(hud.transform, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

            var buttonObject = new GameObject(
                "Grenade Button", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Selectable), typeof(GrenadeButtonView));
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.SetParent(rootRect, false);
            buttonRect.anchorMin = buttonRect.anchorMax = Vector2.zero;
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = Vector2.one * ButtonPlaceholderSize;
            var face = buttonObject.GetComponent<Image>();
            face.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(PipSpritePath);
            face.color = ButtonFaceColor;

            // Interactable, so presses on it belong to the UI, with no transition: the view colours the face.
            var selectable = buttonObject.GetComponent<Selectable>();
            selectable.transition = Selectable.Transition.None;
            selectable.navigation = new Navigation { mode = Navigation.Mode.None };

            var label = HealthBuilder.AddText(
                buttonRect, "Label", "GRENADE", ButtonLabelFontSize, ButtonLabelColor, ButtonLabelOffset, ButtonLabelSize);
            label.raycastTarget = false;

            var pips = BuildGrenadeHud(Vector2.zero, ButtonPipsSize, ButtonPipSize, ButtonPipSpacing, TextAnchor.MiddleCenter);
            var pipsRect = (RectTransform)pips.transform;
            pipsRect.SetParent(buttonRect, false);
            pipsRect.anchorMin = pipsRect.anchorMax = pipsRect.pivot = new Vector2(0.5f, 0.5f);
            pipsRect.anchoredPosition = ButtonPipsOffset;

            var view = buttonObject.GetComponent<GrenadeButtonView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("controls").objectReferenceValue = controls;
            serialized.FindProperty("canvas").objectReferenceValue = hud.GetComponent<Canvas>();
            serialized.FindProperty("button").objectReferenceValue = buttonRect;
            serialized.FindProperty("visibility").objectReferenceValue = buttonObject.GetComponent<CanvasGroup>();
            serialized.FindProperty("face").objectReferenceValue = face;
            serialized.FindProperty("charges").objectReferenceValue = pips;
            serialized.FindProperty("cornerCharges").objectReferenceValue = cornerCharges;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Hidden until the player is on touch.
            var visibility = buttonObject.GetComponent<CanvasGroup>();
            visibility.alpha = 0f;
            visibility.blocksRaycasts = visibility.interactable = false;
            return view;
        }

        private static GrenadeHudView BuildGrenadeHud(Vector2 margin, Vector2 size, float pipSize, float pipSpacing, TextAnchor alignment)
        {
            var hud = GameObject.Find("HUD");
            var grenades = new GameObject("Grenades", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(GrenadeHudView));
            var rect = grenades.GetComponent<RectTransform>();
            rect.SetParent(hud.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-margin.x, margin.y);
            rect.sizeDelta = size;

            var layout = grenades.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = alignment;
            layout.spacing = pipSpacing;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(PipSpritePath);
            var pip = new GameObject("Grenade Pip", typeof(RectTransform), typeof(Image));
            var pipRect = pip.GetComponent<RectTransform>();
            pipRect.SetParent(rect, false);
            pipRect.sizeDelta = new Vector2(pipSize, pipSize);
            var empty = pip.GetComponent<Image>();
            empty.sprite = sprite;
            empty.color = PipEmptyColor;
            empty.raycastTarget = false;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.SetParent(pipRect, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.GetComponent<Image>();
            fillImage.sprite = sprite;
            fillImage.color = PipFullColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Radial360;
            fillImage.fillOrigin = (int)Image.Origin360.Top;
            fillImage.fillClockwise = true;
            fillImage.raycastTarget = false;

            var view = grenades.GetComponent<GrenadeHudView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("pipTemplate").objectReferenceValue = pipRect;
            serialized.FindProperty("pipFillTemplate").objectReferenceValue = fillImage;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static GrenadeView LoadOrCreateGrenadePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GrenadeView>(GrenadePrefabPath);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(WeaponsPrefabsFolder);

            var grenade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            grenade.name = "Grenade";
            Object.DestroyImmediate(grenade.GetComponent<Collider>());

            var body = grenade.GetComponent<MeshRenderer>();
            body.sharedMaterial = LoadOrCreateMaterial("GrenadeGlow", UnlitShaderName, Color.white);
            body.shadowCastingMode = ShadowCastingMode.Off;
            body.receiveShadows = false;

            var trail = grenade.AddComponent<TrailRenderer>();
            trail.sharedMaterial = LoadOrCreateMaterial("GrenadeTrail", ParticlesUnlitShaderName, Color.white);
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.minVertexDistance = TrailMinVertexDistance;
            trail.autodestruct = false;

            var view = grenade.AddComponent<GrenadeView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("body").objectReferenceValue = body;
            serialized.FindProperty("trail").objectReferenceValue = trail;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(grenade, GrenadePrefabPath);
            Object.DestroyImmediate(grenade);
            return prefab.GetComponent<GrenadeView>();
        }

        private static GrenadeExplosionView LoadOrCreateExplosionPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GrenadeExplosionView>(ExplosionPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(WeaponsPrefabsFolder);

            var glow = LoadOrCreateAdditiveParticleMaterial("GrenadeExplosion");
            var explosion = new GameObject("Grenade Explosion", typeof(AudioSource), typeof(CinemachineImpulseSource));

            var fireballs = AddParticles(explosion.transform, "Fireballs", glow, FireballMaxParticles);
            var fireballMain = fireballs.main;
            fireballMain.startLifetime = new ParticleSystem.MinMaxCurve(FireballMinLifetime, FireballMaxLifetime);
            fireballMain.startSpeed = new ParticleSystem.MinMaxCurve(FireballMinSpeed, FireballMaxSpeed);
            fireballMain.startSize = new ParticleSystem.MinMaxCurve(FireballMinSize, FireballMaxSize);
            fireballMain.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            fireballMain.gravityModifier = FireballRise;
            var fireballShape = fireballs.shape;
            fireballShape.shapeType = ParticleSystemShapeType.Hemisphere;
            fireballShape.radius = FireballSpread;
            fireballShape.rotation = new Vector3(-90f, 0f, 0f);
            var fireballColor = fireballs.colorOverLifetime;
            fireballColor.enabled = true;
            fireballColor.color = new ParticleSystem.MinMaxGradient(Fade(FireballHot, FireballWarm, FireballCool));
            var fireballSize = fireballs.sizeOverLifetime;
            fireballSize.enabled = true;
            fireballSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, FireballGrowth));
            var fireballLimit = fireballs.limitVelocityOverLifetime;
            fireballLimit.enabled = true;
            fireballLimit.dampen = FireballDrag;

            var sparks = AddParticles(explosion.transform, "Sparks", glow, SparkMaxParticles);
            var sparkMain = sparks.main;
            sparkMain.startLifetime = new ParticleSystem.MinMaxCurve(SparkMinLifetime, SparkMaxLifetime);
            sparkMain.startSpeed = new ParticleSystem.MinMaxCurve(SparkMinSpeed, SparkMaxSpeed);
            sparkMain.startSize = new ParticleSystem.MinMaxCurve(SparkMinSize, SparkMaxSize);
            sparkMain.startColor = SparkColor;
            sparkMain.gravityModifier = SparkGravity;
            var sparkShape = sparks.shape;
            sparkShape.shapeType = ParticleSystemShapeType.Hemisphere;
            sparkShape.radius = SparkSpread;
            sparkShape.rotation = new Vector3(-90f, 0f, 0f);
            var sparkSize = sparks.sizeOverLifetime;
            sparkSize.enabled = true;
            sparkSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var sparkRenderer = sparks.GetComponent<ParticleSystemRenderer>();
            sparkRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            sparkRenderer.velocityScale = SparkStretch;
            sparkRenderer.lengthScale = 1f;

            var lightObject = new GameObject("Light", typeof(Light));
            lightObject.transform.SetParent(explosion.transform, false);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Point;
            light.shadows = LightShadows.None;
            light.enabled = false;

            var bang = explosion.GetComponent<AudioSource>();
            bang.playOnAwake = false;

            var view = explosion.AddComponent<GrenadeExplosionView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("flashLight").objectReferenceValue = light;
            serialized.FindProperty("fireballs").objectReferenceValue = fireballs;
            serialized.FindProperty("sparks").objectReferenceValue = sparks;
            serialized.FindProperty("bang").objectReferenceValue = bang;
            serialized.FindProperty("impulse").objectReferenceValue = explosion.GetComponent<CinemachineImpulseSource>();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(explosion, ExplosionPrefabPath);
            Object.DestroyImmediate(explosion);
            return prefab.GetComponent<GrenadeExplosionView>();
        }

        private static ParticleSystem AddParticles(Transform parent, string name, Material material, int maxParticles)
        {
            var particlesObject = new GameObject(name, typeof(ParticleSystem));
            particlesObject.transform.SetParent(parent, false);
            var particles = particlesObject.GetComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;

            var emission = particles.emission;
            emission.enabled = false;

            var renderer = particlesObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return particles;
        }

        private static Gradient Fade(Color hot, Color warm, Color cool)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(hot, 0f), new GradientColorKey(warm, 0.3f), new GradientColorKey(cool, 1f) },
                new[] { new GradientAlphaKey(hot.a, 0f), new GradientAlphaKey(warm.a, 0.3f), new GradientAlphaKey(cool.a, 1f) });
            return gradient;
        }

        private static InputActionReference LoadActionReference(string actionPath)
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            var action = actions.FindAction(actionPath, throwIfNotFound: true);
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ControlsPath))
            {
                if (asset is InputActionReference reference && reference.action != null && reference.action.id == action.id)
                {
                    return reference;
                }
            }

            throw new System.InvalidOperationException($"No action reference for {actionPath} in {ControlsPath}.");
        }

        // URP's default particle material (soft round texture), switched to additive so the burst glows.
        private static Material LoadOrCreateAdditiveParticleMaterial(string name)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(AssetDatabase.LoadAssetAtPath<Material>(DefaultParticleMaterialPath)) { name = name };
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
            AssetDatabase.CreateAsset(material, path);
            return material;
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
